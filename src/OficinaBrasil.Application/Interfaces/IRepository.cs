namespace OficinaBrasil.Application;

public interface IRepository<T> where T: class
{
    Task<T?> Get(Guid id);
    Task<List<T>> List();
    Task Add(T entity);
    void Remove(T entity);
}
