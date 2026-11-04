using OficinaBrasil.Domain;
namespace OficinaBrasil.Infrastructure;

public sealed class CustomerRepository : EfRepository<Customer>
{
    public CustomerRepository(WorkshopDbContext db) : base(db)
    {
    }
}
