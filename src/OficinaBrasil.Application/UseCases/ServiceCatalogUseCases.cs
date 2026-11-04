using OficinaBrasil.Domain;
namespace OficinaBrasil.Application;

public class ServiceCatalogUseCases
{
    private readonly IWorkshopData data;

    public ServiceCatalogUseCases(IWorkshopData data)
    {
        this.data = data;
    }

    public async Task<WorkshopService> CreateService(ServiceInput x)
    {
        Checks.Required(x.Name, "Serviço");
        if (x.Price< 0)
            throw new BusinessException("Preço inválido");
        var s = new WorkshopService
        {
            Name = x.Name, Description = x.Description, Price = x.Price, Active = x.Active
        };
        await data.Services.Add(s);
        await data.Save();
        return s;
    }
}
