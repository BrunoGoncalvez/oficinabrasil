using OficinaBrasil.Domain;
namespace OficinaBrasil.Infrastructure;

public sealed class VehicleRepository : EfRepository<Vehicle>
{
    public VehicleRepository(WorkshopDbContext db) : base(db)
    {
    }
}
