using Microsoft.EntityFrameworkCore;
using OficinaBrasil.Application;
namespace OficinaBrasil.Infrastructure;

public class EfRepository<T> : IRepository<T> where T : class
{
    private readonly WorkshopDbContext db;

    public EfRepository(WorkshopDbContext db)
    {
        this.db = db;
    }

    public async Task<T?> Get(Guid id) => await db.Set<T>().FindAsync(id);
    public Task<List<T>> List() => db.Set<T>().AsNoTracking().ToListAsync();
    public async Task Add(T e) => await db.Set<T>().AddAsync(e);
    public void Remove(T e) => db.Set<T>().Remove(e);
}
