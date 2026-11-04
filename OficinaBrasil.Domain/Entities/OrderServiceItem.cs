namespace OficinaBrasil.Domain;

public class OrderServiceItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServiceOrderId { get; set; }
    public Guid WorkshopServiceId { get; set; }
    public WorkshopService WorkshopService { get; set; } = null!;
    public decimal UnitPrice { get; set; }
}
