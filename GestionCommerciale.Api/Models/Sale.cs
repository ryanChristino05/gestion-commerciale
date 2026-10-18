using System.Net.ServerSentEvents;

namespace GestionCommerciale.Api.Models;

public class Sale
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public int ClientId { get; set; }
    public int SellerId { get; set; }
    public decimal TotalAmount { get; set; }
    public SaleStatus Status { get; set; } = SaleStatus.Validated;
    public string? CancelReason { get; set; }

    public Client Client { get; set; } = null!;
    public User Seller { get; set; } = null!;
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}

public enum SaleStatus
{
    Validated,
    Cancelled
}