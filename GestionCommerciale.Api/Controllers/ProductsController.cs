using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionCommerciale.Api.Data;
using GestionCommerciale.Api.Models;

namespace GestionCommerciale.Api.Controllers;

[ApiController]
[Route("products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db)
    {
        _db = db;
    }

    // GET /products?page=1&pageSize=10&search=cafe
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var query = _db.Products
            .Where(p => !p.IsArchived);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.Name.ToLower().Contains(search.ToLower()) ||
                p.Reference.ToLower().Contains(search.ToLower()));

        var total = await query.CountAsync();

        var products = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Reference,
                p.UnitPrice,
                p.Unit,
                p.StockQuantity,
                p.AlertThreshold,
                IsLowStock = p.StockQuantity <= p.AlertThreshold
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, products });
    }

    // GET /products/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _db.Products
            .Where(p => p.Id == id && !p.IsArchived)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Reference,
                p.UnitPrice,
                p.Unit,
                p.StockQuantity,
                p.AlertThreshold,
                IsLowStock = p.StockQuantity <= p.AlertThreshold
            })
            .FirstOrDefaultAsync();

        if (product == null) return NotFound();
        return Ok(product);
    }

    // POST /products
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] ProductDto dto)
    {
        if (_db.Products.Any(p => p.Reference == dto.Reference))
            return BadRequest("Une référence identique existe déjà.");

        var product = new Product
        {
            Name = dto.Name,
            Reference = dto.Reference,
            UnitPrice = dto.UnitPrice,
            Unit = dto.Unit,
            StockQuantity = dto.InitialStock,
            AlertThreshold = dto.AlertThreshold
        };

        _db.Products.Add(product);

        // Mouvement de stock initial si stock > 0
        if (dto.InitialStock > 0)
        {
            _db.StockMovements.Add(new StockMovement
            {
                Product = product,
                Type = MovementType.In,
                Quantity = dto.InitialStock,
                Reason = "Stock initial",
                Date = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, new { product.Id });
    }

    // PUT /products/5
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductDto dto)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null || product.IsArchived) return NotFound();

        if (_db.Products.Any(p => p.Reference == dto.Reference && p.Id != id))
            return BadRequest("Une référence identique existe déjà.");

        product.Name = dto.Name;
        product.Reference = dto.Reference;
        product.UnitPrice = dto.UnitPrice;
        product.Unit = dto.Unit;
        product.AlertThreshold = dto.AlertThreshold;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /products/5 (archive uniquement)
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Archive(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null || product.IsArchived) return NotFound();

        product.IsArchived = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // GET /products/5/movements
    [HttpGet("{id}/movements")]
    public async Task<IActionResult> GetMovements(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null || product.IsArchived) return NotFound();

        var movements = await _db.StockMovements
            .Where(m => m.ProductId == id)
            .OrderByDescending(m => m.Date)
            .Select(m => new
            {
                m.Id,
                m.Type,
                m.Quantity,
                m.Reason,
                m.Date,
                m.SaleId
            })
            .ToListAsync();

        return Ok(movements);
    }
}

public class ProductDto
{
    public string Name { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int InitialStock { get; set; }
    public int AlertThreshold { get; set; }
}