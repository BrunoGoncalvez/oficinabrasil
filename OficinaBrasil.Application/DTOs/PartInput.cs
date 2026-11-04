using OficinaBrasil.Domain;
namespace OficinaBrasil.Application;

public class PartInput
{
    public string Name { get; set; } = "";
    public string Sku { get; set; } = "";
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public PartInput()
    {
    }

    public PartInput(string name, string sku, string? description, decimal unitPrice, int quantity)
    {
        Name = name;
        Sku = sku;
        Description = description;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }
}
