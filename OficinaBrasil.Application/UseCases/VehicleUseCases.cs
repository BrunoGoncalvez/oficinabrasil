using OficinaBrasil.Domain;
namespace OficinaBrasil.Application;

public class VehicleUseCases
{
    private readonly IWorkshopData data;

    public VehicleUseCases(IWorkshopData data)
    {
        this.data = data;
    }

    public async Task<Vehicle> CreateVehicle(VehicleInput input)
    {
        if (await data.Customers.Get(input.CustomerId) == null)
            throw new KeyNotFoundException("Cliente não encontrado");
        var plate = new LicensePlate(input.Plate).Value;
        if (await data.FindVehicleByPlate(plate) != null)
            throw new BusinessException("Placa já cadastrada");
        Checks.Required(input.Brand, "Marca");
        Checks.Required(input.Model, "Modelo");
        if (input.Year< 1900 || input.Year> DateTime.UtcNow.Year + 1)
            throw new BusinessException("Ano inválido");
        var v = new Vehicle
        {
            CustomerId = input.CustomerId, Plate = plate, Brand = input.Brand, Model = input.Model, Year = input.Year
        };
        await data.Vehicles.Add(v);
        await data.Save();
        return v;
    }
}
