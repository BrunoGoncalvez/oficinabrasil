namespace OficinaBrasil.Domain;

public class Vehicle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string Plate { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Model { get; set; } = "";
    public int Year { get; set; }
    public List<ServiceOrder> Orders { get; set; } = new();
}
