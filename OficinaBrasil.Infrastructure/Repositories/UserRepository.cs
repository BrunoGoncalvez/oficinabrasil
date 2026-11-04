using OficinaBrasil.Domain;
namespace OficinaBrasil.Infrastructure;

public sealed class UserRepository : EfRepository<User>
{
    public UserRepository(WorkshopDbContext db) : base(db)
    {
    }
}
