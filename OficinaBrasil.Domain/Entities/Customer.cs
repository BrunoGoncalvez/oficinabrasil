namespace OficinaBrasil.Domain;

public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Document { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public List<Vehicle> Vehicles { get; set; } = new();
}
