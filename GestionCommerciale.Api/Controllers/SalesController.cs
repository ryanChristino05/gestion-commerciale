using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GestionCommerciale.Api.Data;
using GestionCommerciale.Api.Models;

namespace GestionCommerciale.Api.Controllers;

[ApiController]
[Route("sales")]
[Authorize]
public class SalesController : ControllerBase
{
    private readonly AppDbContext _db;

    public SalesController(AppDbContext db)
    {
        _db = db;
    }

    // GET /sales
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role);

        var query = _db.Sales
            .Include(s => s.Client)
            .Include(s => s.Seller)
            .Include(s => s.Items)
            .AsQueryable();

        // Seller voit uniquement ses propres ventes
        if (role == "Seller")
            query = query.Where(s => s.SellerId == userId);

        var sales = await query
            .OrderByDescending(s => s.Date)
            .Select(s => new
            {
                s.Id,
                s.Number,
                s.Date,
                s.TotalAmount,
                s.Status,
                Client = s.Client.Name,
                Seller = s.Seller.Username,
                ItemCount = s.Items.Count
            })
            .ToListAsync();

        return Ok(sales);
    }

    // GET /sales/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role);

        var sale = await _db.Sales
            .Include(s => s.Client)
            .Include(s => s.Seller)
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sale == null) return NotFound();

        // Seller ne peut pas voir les ventes des autres
        if (role == "Seller" && sale.SellerId != userId)
            return Forbid();

        return Ok(new
        {
            sale.Id,
            sale.Number,
            sale.Date,
            sale.TotalAmount,
            sale.Status,
            sale.CancelReason,
            Client = sale.Client.Name,
            Seller = sale.Seller.Username,
            Items = sale.Items.Select(i => new
            {
                i.Id,
                Product = i.Product.Name,
                i.Quantity,
                i.UnitPrice,
                i.Subtotal
            })
        });
    }

    // POST /sales
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSaleDto dto)
    {
        var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Vérifier que le client existe
        var client = await _db.Clients.FindAsync(dto.ClientId);
        if (client == null || client.IsArchived)
            return BadRequest("Client introuvable.");
        // Vérifier que chaque quantité est strictement positive
        if (dto.Items.Any(item => item.Quantity <= 0))
            return BadRequest("La quantité doit être supérieure à zéro.");
        // Vérifier que les lignes ne sont pas vides
        if (dto.Items == null || dto.Items.Count == 0)
            return BadRequest("La vente doit contenir au moins une ligne.");

        // Charger tous les produits concernés
        var productIds = dto.Items.Select(i => i.ProductId).ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id) && !p.IsArchived)
            .ToListAsync();

        // Vérifier que tous les produits existent
        if (products.Count != productIds.Distinct().Count())
            return BadRequest("Un ou plusieurs produits sont introuvables.");

        // Vérifier le stock pour chaque ligne
        foreach (var item in dto.Items)
        {
            var product = products.First(p => p.Id == item.ProductId);
            if (product.StockQuantity < item.Quantity)
                return BadRequest(
                    $"Stock insuffisant pour '{product.Name}' " +
                    $"(disponible : {product.StockQuantity}, demandé : {item.Quantity})."
                );
        }

        // Générer le numéro de vente
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await _db.Sales.CountAsync() + 1;
        var number = $"VNT-{today}-{count:D4}";

        // Créer la vente
        var sale = new Sale
        {
            Number = number,
            Date = DateTime.UtcNow,
            ClientId = dto.ClientId,
            SellerId = sellerId,
            Status = SaleStatus.Validated
        };

        decimal total = 0;

        foreach (var itemDto in dto.Items)
        {
            var product = products.First(p => p.Id == itemDto.ProductId);
            var subtotal = product.UnitPrice * itemDto.Quantity;
            total += subtotal;

            // Ligne de vente
            sale.Items.Add(new SaleItem
            {
                ProductId = product.Id,
                Quantity = itemDto.Quantity,
                UnitPrice = product.UnitPrice,
                Subtotal = subtotal
            });

            // Décrémenter le stock
            product.StockQuantity -= itemDto.Quantity;

            // Mouvement de stock (sortie)
            _db.StockMovements.Add(new StockMovement
            {
                ProductId = product.Id,
                Type = MovementType.Out,
                Quantity = itemDto.Quantity,
                Reason = $"Vente {number}",
                Date = DateTime.UtcNow,
                Sale = sale
            });
        }

        sale.TotalAmount = total;
        _db.Sales.Add(sale);

        // Transaction atomique : tout ou rien
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = sale.Id }, new
        {
            sale.Id,
            sale.Number,
            sale.TotalAmount,
            sale.Status
        });
    }

    // POST /sales/5/cancel
    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelDto dto)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (role != "Admin")
            return Forbid();
        var sale = await _db.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sale == null) return NotFound();

        if (sale.Status == SaleStatus.Cancelled)
            return BadRequest("Cette vente est déjà annulée.");

        // Remettre le stock pour chaque ligne
        foreach (var item in sale.Items)
        {
            var product = await _db.Products.FindAsync(item.ProductId);
            if (product != null)
            {
                product.StockQuantity += item.Quantity;

                _db.StockMovements.Add(new StockMovement
                {
                    ProductId = item.ProductId,
                    Type = MovementType.In,
                    Quantity = item.Quantity,
                    Reason = $"Annulation vente {sale.Number}",
                    Date = DateTime.UtcNow
                });
            }
        }

        sale.Status = SaleStatus.Cancelled;
        sale.CancelReason = dto.Reason;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Vente annulée, stock remis." });
    }
}

public class CreateSaleDto
{
    public int ClientId { get; set; }
    public List<SaleItemDto> Items { get; set; } = new();
}

public class SaleItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class CancelDto
{
    public string Reason { get; set; } = string.Empty;
}