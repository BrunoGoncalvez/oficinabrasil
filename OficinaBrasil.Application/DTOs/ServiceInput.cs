using OficinaBrasil.Domain;
namespace OficinaBrasil.Application;

public class ServiceInput
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool Active { get; set; } = true;
    public ServiceInput()
    {
    }

    public ServiceInput(string name, string? description, decimal price, bool active = true)
    {
        Name = name;
        Description = description;
        Price = price;
        Active = active;
    }
}
