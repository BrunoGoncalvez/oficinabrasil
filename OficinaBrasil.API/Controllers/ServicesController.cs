using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OficinaBrasil.Application;
using OficinaBrasil.Domain;
namespace OficinaBrasil.API.Controllers;

[ApiController]
[Route("api/services")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly IWorkshopData d;
    private readonly ServiceCatalogUseCases u;
    public ServicesController(IWorkshopData d, ServiceCatalogUseCases u)
    {
        this.d = d;
        this.u = u;
    }

    [HttpGet]
    [Authorize(Roles = "Atendente,Mecanico,Administrador")]
    public async Task<IActionResult> List() => Ok(await d.Services.List());
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Atendente,Mecanico,Administrador")]
    public async Task<IActionResult> Get(Guid id)
    {
        var s = await d.Services.Get(id);
        return s == null? NotFound(): Ok(s);
    }

    [HttpPost]
    [Authorize(Roles = "Atendente,Administrador")]
    public async Task<IActionResult> Create(ServiceInput x)
    {
        var s = await u.CreateService(x);
        return CreatedAtAction(nameof (Get), new
        {
            id = s.Id
        }, s);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Atendente,Administrador")]
    public async Task<IActionResult> Update(Guid id, ServiceInput x)
    {
        var s = await d.Services.Get(id);
        if (s == null)
            return NotFound();
        Checks.Required(x.Name, "Serviço");
        if (x.Price< 0)
            throw new BusinessException("Preço inválido");
        s.Name = x.Name;
        s.Description = x.Description;
        s.Price = x.Price;
        s.Active = x.Active;
        await d.Save();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Atendente,Administrador")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var s = await d.Services.Get(id);
        if (s == null)
            return NotFound();
        d.Services.Remove(s);
        await d.Save();
        return NoContent();
    }
}
