using OficinaBrasil.Domain;
namespace OficinaBrasil.Infrastructure;

public sealed class PartRepository : EfRepository<Part>
{
    public PartRepository(WorkshopDbContext db) : base(db)
    {
    }
}
