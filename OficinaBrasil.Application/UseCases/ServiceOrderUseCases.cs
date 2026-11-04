using OficinaBrasil.Domain;
namespace OficinaBrasil.Application;

public class ServiceOrderUseCases
{
    private readonly IWorkshopData data;

    public ServiceOrderUseCases(IWorkshopData data)
    {
        this.data = data;
    }

    public async Task<ServiceOrder> CreateOrder(OrderInput x)
    {
        // [Etapa 1: confirmar o veículo e impedir itens duplicados na abertura]
        if (await data.Vehicles.Get(x.VehicleId) == null)
            throw new KeyNotFoundException("Veículo não encontrado");
        x.Services ??= new();
        if (x.Services.Select(s => s.ServiceId).Distinct().Count() != x.Services.Count)
            throw new BusinessException("Serviço duplicado");
        if (x.Parts != null && x.Parts.GroupBy(p => p.PartId).Any(g => g.Count()> 1))
            throw new BusinessException("Peça duplicada");
        // [Etapa 2: montar a OS com os itens preliminares e os preços do catálogo]
        var o = new ServiceOrder
        {
            VehicleId = x.VehicleId, Notes = x.Notes
        };
        foreach(var si in x.Services)
        {
            var s = await data.Services.Get(si.ServiceId) ?? throw new KeyNotFoundException("Serviço não encontrado");
            if (!s.Active)
                throw new BusinessException("Serviço inativo");
            o.Services.Add(new OrderServiceItem
            {
                ServiceOrderId = o.Id, WorkshopServiceId = s.Id, UnitPrice = s.Price
            });
        }
        foreach(var pi in x.Parts ?? new())
        {
            var p = await data.Parts.Get(pi.PartId) ?? throw new KeyNotFoundException("Peça não encontrada");
            if (pi.Quantity <= 0)
                throw new BusinessException("Quantidade inválida");
            o.Parts.Add(new OrderPartItem
            {
                ServiceOrderId = o.Id, PartId = p.Id, Quantity = pi.Quantity, UnitPrice = p.UnitPrice
            });
        }
        // [Etapa 3: salvar a OS no estado inicial RECEBIDA]
        await data.Orders.Add(o);
        await data.Save();
        return o;
    }

    public async Task<ServiceOrder> ChangeStatus(Guid id, ServiceOrderStatus next)
    {
        // [Etapa 1: impedir que a alteração de status contorne a decisão do orçamento]
        if (next is ServiceOrderStatus.AGUARDANDO_APROVACAO or ServiceOrderStatus.APROVADA)
            throw new BusinessException("Use o cadastro de orçamento e o link de decisão enviado ao cliente");
        var o = await data.GetOrderWithItems(id) ?? throw new KeyNotFoundException("OS não encontrada");
        if (o.Status == ServiceOrderStatus.AGUARDANDO_APROVACAO && next == ServiceOrderStatus.CANCELADA)
            throw new BusinessException("A reprovação deve ser registrada pelo link do orçamento");
        // [Etapa 2: conferir o estoque antes de iniciar a execução]
        if (next == ServiceOrderStatus.EM_EXECUCAO)
        {
            foreach(var item in o.Parts)
            {
                var p = await data.Parts.Get(item.PartId) ?? throw new KeyNotFoundException("Peça não encontrada");
                if (p.Quantity<item.Quantity)
                    throw new BusinessException($"Estoque insuficiente para {p.Name}");
            }
        }
        // [Etapa 3: validar a transição e registrar a baixa das peças ao iniciar a execução]
        o.Transition(next);
        if (next == ServiceOrderStatus.EM_EXECUCAO)
        {
            foreach(var item in o.Parts)
            {
                var p =(await data.Parts.Get(item.PartId))!;
                p.AdjustStock(- item.Quantity);
                await data.Movements.Add(new StockMovement
                {
                    PartId = p.Id, Delta = - item.Quantity, Reason = $"OS {o.Id}"
                });
            }
        }
        // [Etapa 4: salvar o estado, o saldo e as movimentações na mesma transação]
        await data.Save();
        return o;
    }

    public async Task<object> ExecutionAverage()
    {
        var orders = await data.ListOrders();
        var completed = orders.Where(o => o.StartedAt.HasValue && o.FinishedAt.HasValue).ToList();
        return new
        {
            Count = completed.Count, AverageHours = completed.Count == 0? 0: Math.Round(completed.Average(o =>(o.FinishedAt!.Value - o.StartedAt!.Value).TotalHours), 2)
        };
    }
}
