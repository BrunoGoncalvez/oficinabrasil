using OficinaBrasil.Domain;
namespace OficinaBrasil.Infrastructure;

public sealed class ServiceOrderRepository : EfRepository<ServiceOrder>
{
    public ServiceOrderRepository(WorkshopDbContext db) : base(db)
    {
    }
}
