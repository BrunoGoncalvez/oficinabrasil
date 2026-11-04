using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OficinaBrasil.Application;
using OficinaBrasil.Domain;
namespace OficinaBrasil.API.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Roles = "Atendente,Administrador")]
public class CustomersController : ControllerBase
{
    private readonly IWorkshopData d;
    private readonly CustomerUseCases u;
    public CustomersController(IWorkshopData d, CustomerUseCases u)
    {
        this.d = d;
        this.u = u;
    }

    [HttpGet]
    public async Task<IActionResult> List() => Ok((await d.Customers.List()).Select(c => new
    {
        c.Id, c.Name, c.Document, c.Email, c.Phone
    }));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var c = await d.Customers.Get(id);
        return c == null? NotFound(): Ok(new
        {
            c.Id, c.Name, c.Document, c.Email, c.Phone
        });
    }

    [HttpGet("by-document")]
    public async Task<IActionResult> GetByDocument([FromQuery] string document)
    {
        var c = await u.FindByDocument(document);
        return c == null ? NotFound() : Ok(new
        {
            c.Id, c.Name, c.Document, c.Email, c.Phone
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CustomerInput x)
    {
        var c = await u.CreateCustomer(x);
        return CreatedAtAction(nameof (Get), new
        {
            id = c.Id
        }, new
        {
            c.Id, c.Name, c.Document
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CustomerInput x)
    {
        var c = await d.Customers.Get(id);
        if (c == null)
            return NotFound();
        var doc = new DocumentNumber(x.Document).Value;
        var existing = await d.FindCustomerByDocument(doc);
        if (existing != null && existing.Id != id)
            throw new BusinessException("Documento duplicado");
        Checks.Required(x.Name, "Nome");
        c.Name = x.Name;
        c.Document = doc;
        c.Email = x.Email;
        c.Phone = x.Phone;
        await d.Save();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var c = await d.Customers.Get(id);
        if (c == null)
            return NotFound();
        d.Customers.Remove(c);
        await d.Save();
        return NoContent();
    }
}
