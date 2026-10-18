using System.Net.ServerSentEvents;

namespace GestionCommerciale.Api.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int AlertThreshold { get; set; }
    public bool IsArchived { get; set; } = false;

    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}