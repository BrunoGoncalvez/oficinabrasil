using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OficinaBrasil.Application;
using OficinaBrasil.Domain;
namespace OficinaBrasil.API.Controllers;

[ApiController]
[Route("api/vehicles")]
[Authorize(Roles = "Atendente,Administrador")]
public class VehiclesController : ControllerBase
{
    private readonly IWorkshopData d;
    private readonly VehicleUseCases u;
    public VehiclesController(IWorkshopData d, VehicleUseCases u)
    {
        this.d = d;
        this.u = u;
    }

    [HttpGet]
    public async Task<IActionResult> List() => Ok((await d.Vehicles.List()).Select(v => new
    {
        v.Id, v.CustomerId, v.Plate, v.Brand, v.Model, v.Year
    }));
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var v = await d.Vehicles.Get(id);
        return v == null? NotFound(): Ok(new
        {
            v.Id, v.CustomerId, v.Plate, v.Brand, v.Model, v.Year
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(VehicleInput x)
    {
        var v = await u.CreateVehicle(x);
        return CreatedAtAction(nameof (Get), new
        {
            id = v.Id
        }, new
        {
            v.Id, v.CustomerId, v.Plate
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, VehicleInput x)
    {
        var v = await d.Vehicles.Get(id);
        if (v == null)
            return NotFound();
        if (await d.Customers.Get(x.CustomerId) == null)
            return NotFound("Cliente não encontrado");
        var plate = new LicensePlate(x.Plate).Value;
        var dup = await d.FindVehicleByPlate(plate);
        if (dup != null && dup.Id != id)
            throw new BusinessException("Placa duplicada");
        Checks.Required(x.Brand, "Marca");
        Checks.Required(x.Model, "Modelo");
        if (x.Year< 1900 || x.Year> DateTime.UtcNow.Year + 1)
            throw new BusinessException("Ano inválido");
        v.CustomerId = x.CustomerId;
        v.Plate = plate;
        v.Brand = x.Brand;
        v.Model = x.Model;
        v.Year = x.Year;
        await d.Save();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var v = await d.Vehicles.Get(id);
        if (v == null)
            return NotFound();
        d.Vehicles.Remove(v);
        await d.Save();
        return NoContent();
    }
}
