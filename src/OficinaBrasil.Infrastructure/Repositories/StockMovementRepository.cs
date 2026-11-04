using OficinaBrasil.Domain;
namespace OficinaBrasil.Infrastructure;

public sealed class StockMovementRepository : EfRepository<StockMovement>
{
    public StockMovementRepository(WorkshopDbContext db) : base(db)
    {
    }
}
