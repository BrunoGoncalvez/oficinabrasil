namespace OficinaBrasil.Domain;

public class ServiceOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public string? Notes { get; set; }
    public ServiceOrderStatus Status
    {
        get;
        private set;
    } = ServiceOrderStatus.RECEBIDA;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt
    {
        get;
        private set;
    }
    public DateTime? FinishedAt
    {
        get;
        private set;
    }
    public DateTime? DeliveredAt
    {
        get;
        private set;
    }
    public List<OrderServiceItem> Services { get; set; } = new();
    public List<OrderPartItem> Parts { get; set; } = new();
    public Estimate? Estimate { get; set; }
    public decimal Total => Services.Sum(x => x.UnitPrice) + Parts.Sum(x => x.UnitPrice * x.Quantity);
    public void Transition(ServiceOrderStatus next)
    {
        bool allowed = (Status, next) switch
        {
            (ServiceOrderStatus.RECEBIDA, ServiceOrderStatus.EM_DIAGNOSTICO) => true, 
            (ServiceOrderStatus.EM_DIAGNOSTICO, ServiceOrderStatus.AGUARDANDO_APROVACAO) => true, 
            (ServiceOrderStatus.AGUARDANDO_APROVACAO, ServiceOrderStatus.APROVADA) => true, 
            (ServiceOrderStatus.APROVADA, ServiceOrderStatus.EM_EXECUCAO) => true, 
            (ServiceOrderStatus.EM_EXECUCAO, ServiceOrderStatus.FINALIZADA) => true, 
            (ServiceOrderStatus.FINALIZADA, ServiceOrderStatus.ENTREGUE) => true, 
            (ServiceOrderStatus.RECEBIDA, ServiceOrderStatus.CANCELADA) => true, 
            (ServiceOrderStatus.EM_DIAGNOSTICO, ServiceOrderStatus.CANCELADA) => true, 
            (ServiceOrderStatus.AGUARDANDO_APROVACAO, ServiceOrderStatus.CANCELADA) => true, 
            (ServiceOrderStatus.APROVADA, ServiceOrderStatus.CANCELADA) => true, 
            _ => false
        };
        if (!allowed)
            throw new BusinessException($"Transição inválida: {Status} -> {next}");
        Status = next;
        if (next == ServiceOrderStatus.EM_EXECUCAO) StartedAt = DateTime.UtcNow;
        if (next == ServiceOrderStatus.FINALIZADA) FinishedAt = DateTime.UtcNow;
        if (next == ServiceOrderStatus.ENTREGUE) DeliveredAt = DateTime.UtcNow;
    }
}
