using Microsoft.EntityFrameworkCore;
using OficinaBrasil.Application;
using OficinaBrasil.Domain;
namespace OficinaBrasil.Infrastructure;

public class WorkshopData: IWorkshopData
{
    private readonly WorkshopDbContext db;
    public WorkshopData(WorkshopDbContext db)
    {
        this.db = db;
    }

    public IRepository<Customer> Customers => new CustomerRepository(db);
    public IRepository<Estimate> Estimates => new EfRepository<Estimate>(db);
    public IRepository<Vehicle> Vehicles => new VehicleRepository(db);
    public IRepository<User> Users => new UserRepository(db);
    public IRepository<WorkshopService> Services => new WorkshopServiceRepository(db);
    public IRepository<Part> Parts => new PartRepository(db);
    public IRepository<ServiceOrder> Orders => new ServiceOrderRepository(db);
    public IRepository<StockMovement> Movements => new StockMovementRepository(db);
    public Task<User?> FindUserByEmail(string email) => db.Users.FirstOrDefaultAsync(x => x.Email == email);
    public Task<Customer?> FindCustomerByDocument(string doc) => db.Customers.FirstOrDefaultAsync(x => x.Document == doc);
    public Task<Vehicle?> FindVehicleByPlate(string plate) => db.Vehicles.FirstOrDefaultAsync(x => x.Plate == plate);
    public Task<Part?> FindPartBySku(string sku) => db.Parts.FirstOrDefaultAsync(x => x.Sku == sku);
    public Task<ServiceOrder?> GetOrderWithItems(Guid id) => db.ServiceOrders.Include(x => x.Services).Include(x => x.Parts).Include(x => x.Estimate).AsSplitQuery().FirstOrDefaultAsync(x => x.Id == id);
    public Task<List<ServiceOrder>> ListOrders() => db.ServiceOrders.AsNoTracking().Include(x => x.Services).Include(x => x.Parts).Include(x => x.Estimate).AsSplitQuery().ToListAsync();
    public Task<List<ServiceOrder>> CustomerOrders(string document) => db.ServiceOrders.AsNoTracking().Include(x => x.Vehicle).Include(x => x.Services).Include(x => x.Parts).AsSplitQuery().Where(x => x.Vehicle.Customer.Document == document).ToListAsync();
    public Task Save() => db.SaveChangesAsync();
}
