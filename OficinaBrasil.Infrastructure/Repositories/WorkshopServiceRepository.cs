using OficinaBrasil.Domain;
namespace OficinaBrasil.Infrastructure;

public sealed class WorkshopServiceRepository : EfRepository<WorkshopService>
{
    public WorkshopServiceRepository(WorkshopDbContext db) : base(db)
    {
    }
}
