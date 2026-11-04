using OficinaBrasil.Domain;
namespace OficinaBrasil.Application;

public class OrderInput
{
    public Guid VehicleId { get; set; }
    public string? Notes { get; set; }
    public List<OrderServiceInput> Services { get; set; } = new();
    public List<OrderPartInput> Parts { get; set; } = new();
    public OrderInput()
    {
    }

    public OrderInput(Guid vehicleId, string? notes, List<OrderServiceInput> services, List<OrderPartInput> parts)
    {
        VehicleId = vehicleId;
        Notes = notes;
        Services = services;
        Parts = parts;
    }
}
