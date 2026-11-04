namespace OficinaBrasil.Domain;

public class StockMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PartId { get; set; }
    public Part Part { get; set; } = null!;
    public int Delta { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
    public string Reason { get; set; } = "";
}
