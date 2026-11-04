namespace OficinaBrasil.Domain;

public class Part
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Sku { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public void AdjustStock(int delta)
    {
        var updated = (long)Quantity + delta;
        if (updated < 0 || updated > int.MaxValue)
            throw new BusinessException("Estoque insuficiente");
        Quantity = (int)updated;
    }
}
