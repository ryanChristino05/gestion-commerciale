using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionCommerciale.Api.Data;
using GestionCommerciale.Api.Models;

namespace GestionCommerciale.Api.Controllers;

[ApiController]
[Route("stock")]
[Authorize(Roles = "Admin")]
public class StockController : ControllerBase
{
    private readonly AppDbContext _db;

    public StockController(AppDbContext db)
    {
        _db = db;
    }

    // POST /stock/in
    [HttpPost("in")]
    public async Task<IActionResult> StockIn([FromBody] StockInDto dto)
    {
        var product = await _db.Products.FindAsync(dto.ProductId);
        if (product == null || product.IsArchived)
            return NotFound("Produit introuvable.");

        if (dto.Quantity <= 0)
            return BadRequest("La quantité doit être supérieure à 0.");

        product.StockQuantity += dto.Quantity;

        _db.StockMovements.Add(new StockMovement
        {
            ProductId = dto.ProductId,
            Type = MovementType.In,
            Quantity = dto.Quantity,
            Reason = dto.Reason,
            Date = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Stock mis à jour.",
            productId = product.Id,
            newStock = product.StockQuantity
        });
    }
}

public class StockInDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
}