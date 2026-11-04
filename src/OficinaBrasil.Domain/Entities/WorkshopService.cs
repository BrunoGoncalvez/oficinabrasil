namespace OficinaBrasil.Domain;

public class WorkshopService
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool Active { get; set; } = true;
}
