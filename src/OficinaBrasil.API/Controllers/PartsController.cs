using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OficinaBrasil.Application;
using OficinaBrasil.Domain;
namespace OficinaBrasil.API.Controllers;

[ApiController]
[Route("api/parts")]
[Authorize]
public class PartsController : ControllerBase
{
    private readonly IWorkshopData d;
    private readonly InventoryUseCases u;
    public PartsController(IWorkshopData d, InventoryUseCases u)
    {
        this.d = d;
        this.u = u;
    }

    [HttpGet]
    [Authorize(Roles = "Estoquista,Mecanico,Administrador")]
    public async Task<IActionResult> List() => Ok(await d.Parts.List());
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Estoquista,Mecanico,Administrador")]
    public async Task<IActionResult> Get(Guid id)
    {
        var p = await d.Parts.Get(id);
        return p == null? NotFound(): Ok(p);
    }

    [HttpPost]
    [Authorize(Roles = "Estoquista,Administrador")]
    public async Task<IActionResult> Create(PartInput x)
    {
        var p = await u.CreatePart(x);
        return CreatedAtAction(nameof (Get), new
        {
            id = p.Id
        }, p);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Estoquista,Administrador")]
    public async Task<IActionResult> Update(Guid id, PartInput x)
    {
        var p = await d.Parts.Get(id);
        if (p == null)
            return NotFound();
        Checks.Required(x.Name, "Peça");
        Checks.Required(x.Sku, "SKU");
        if (x.UnitPrice< 0)
            throw new BusinessException("Preço inválido");
        var dup = await d.FindPartBySku(x.Sku);
        if (dup != null && dup.Id != id)
            throw new BusinessException("SKU duplicado");
        p.Name = x.Name;
        p.Sku = x.Sku;
        p.Description = x.Description;
        p.UnitPrice = x.UnitPrice;
        await d.Save();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Estoquista,Administrador")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var p = await d.Parts.Get(id);
        if (p == null)
            return NotFound();
        d.Parts.Remove(p);
        await d.Save();
        return NoContent();
    }

    [HttpPost("{id:guid}/stock")]
    [Authorize(Roles = "Estoquista,Administrador")]
    public async Task<IActionResult> Stock(Guid id, StockInput x) => Ok(await u.MoveStock(id, x));
}
