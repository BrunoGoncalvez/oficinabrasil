using OficinaBrasil.Domain;
namespace OficinaBrasil.Application;

public class OrderServiceInput
{
    public Guid ServiceId { get; set; }
    public OrderServiceInput()
    {
    }

    public OrderServiceInput(Guid serviceId)
    {
        ServiceId = serviceId;
    }
}
