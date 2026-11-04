using OficinaBrasil.Domain;
namespace OficinaBrasil.Application;

public class InventoryUseCases
{
    private readonly IWorkshopData data;

    public InventoryUseCases(IWorkshopData data)
    {
        this.data = data;
    }

    public async Task<Part> CreatePart(PartInput x)
    {
        Checks.Required(x.Name, "Peça");
        Checks.Required(x.Sku, "SKU");
        if (x.UnitPrice< 0 || x.Quantity< 0)
            throw new BusinessException("Valor ou quantidade inválido");
        if (await data.FindPartBySku(x.Sku) != null)
            throw new BusinessException("SKU já cadastrado");
        var p = new Part
        {
            Name = x.Name, Sku = x.Sku, Description = x.Description, UnitPrice = x.UnitPrice, Quantity = x.Quantity
        };
        await data.Parts.Add(p);
        if (x.Quantity> 0) await data.Movements.Add(new StockMovement
        {
            PartId = p.Id, Delta = x.Quantity, Reason = "Saldo inicial"
        });
        await data.Save();
        return p;
    }

    public async Task<Part> MoveStock(Guid id, StockInput x)
    {
        var p = await data.Parts.Get(id) ?? throw new KeyNotFoundException("Peça não encontrada");
        Checks.Required(x.Reason, "Motivo");
        if (x.Delta == 0)
            throw new BusinessException("Movimentação não pode ser zero");
        p.AdjustStock(x.Delta);
        await data.Movements.Add(new StockMovement
        {
            PartId = p.Id, Delta = x.Delta, Reason = x.Reason
        });
        await data.Save();
        return p;
    }
}
