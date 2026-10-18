namespace GestionCommerciale.Api.Models;

public class StockMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public MovementType Type { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public int? SaleId { get; set; }

    public Product Product { get; set; } = null!;
    public Sale? Sale { get; set; }
}

public enum MovementType
{
    In,
    Out
}