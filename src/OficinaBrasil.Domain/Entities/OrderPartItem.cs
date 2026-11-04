namespace OficinaBrasil.Domain;

public class OrderPartItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServiceOrderId { get; set; }
    public Guid PartId { get; set; }
    public Part Part { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
