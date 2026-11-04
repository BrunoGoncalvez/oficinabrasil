using OficinaBrasil.Domain;
namespace OficinaBrasil.Application;

public interface IWorkshopData
{
    IRepository<Estimate> Estimates { get; }
    IRepository<Customer> Customers
    {
        get;
    }
    IRepository<Vehicle> Vehicles
    {
        get;
    }
    IRepository<User> Users
    {
        get;
    }
    IRepository<WorkshopService> Services
    {
        get;
    }
    IRepository<Part> Parts
    {
        get;
    }
    IRepository<ServiceOrder> Orders
    {
        get;
    }
    IRepository<StockMovement> Movements
    {
        get;
    }
    Task<User?> FindUserByEmail(string email);
    Task<Customer?> FindCustomerByDocument(string doc);
    Task<Vehicle?> FindVehicleByPlate(string plate);
    Task<Part?> FindPartBySku(string sku);
    Task<ServiceOrder?> GetOrderWithItems(Guid id);
    Task<List<ServiceOrder>> ListOrders();
    Task<List<ServiceOrder>> CustomerOrders(string document);
    Task Save();
}
