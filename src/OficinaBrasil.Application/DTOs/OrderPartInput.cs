using OficinaBrasil.Domain;
namespace OficinaBrasil.Application;

public class OrderPartInput
{
    public Guid PartId { get; set; }
    public int Quantity { get; set; }
    public OrderPartInput()
    {
    }

    public OrderPartInput(Guid partId, int quantity)
    {
        PartId = partId;
        Quantity = quantity;
    }
}
