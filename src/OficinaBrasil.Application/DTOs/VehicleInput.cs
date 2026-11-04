using OficinaBrasil.Domain;
namespace OficinaBrasil.Application;

public class VehicleInput
{
    public Guid CustomerId { get; set; }
    public string Plate { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Model { get; set; } = "";
    public int Year { get; set; }
    public VehicleInput()
    {
    }

    public VehicleInput(Guid customerId, string plate, string brand, string model, int year)
    {
        CustomerId = customerId;
        Plate = plate;
        Brand = brand;
        Model = model;
        Year = year;
    }
}
