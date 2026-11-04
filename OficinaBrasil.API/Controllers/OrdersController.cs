using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OficinaBrasil.Application;
using OficinaBrasil.Domain;
namespace OficinaBrasil.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IWorkshopData d;
    private readonly ServiceOrderUseCases u;
    public OrdersController(IWorkshopData d, ServiceOrderUseCases u)
    {
        this.d = d;
        this.u = u;
    }

    [HttpGet]
    [Authorize(Roles = "Atendente,Mecanico,Administrador")]
    public async Task<IActionResult> List() => Ok((await d.ListOrders()).Select(Format));


    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Atendente,Mecanico,Administrador")]
    public async Task<IActionResult> Get(Guid id)
    {
        var o = await d.GetOrderWithItems(id);
        return o == null? NotFound(): Ok(Format(o));
    }

    [HttpPost]
    [Authorize(Roles = "Atendente,Administrador")]
    public async Task<IActionResult> Create(OrderInput x)
    {
        var o = await u.CreateOrder(x);
        return CreatedAtAction(nameof (Get), new
        {
            id = o.Id
        }, Format(o));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Atendente,Mecanico,Administrador")]
    public async Task<IActionResult> Status(Guid id, StatusInput x)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var mechanicStates = new[]
        {
            ServiceOrderStatus.EM_DIAGNOSTICO, ServiceOrderStatus.AGUARDANDO_APROVACAO, ServiceOrderStatus.EM_EXECUCAO, ServiceOrderStatus.FINALIZADA
        };
        if (role == "Mecanico" && !mechanicStates.Contains(x.Status))
            return Forbid();
        if (role == "Atendente" && mechanicStates.Contains(x.Status) && x.Status != ServiceOrderStatus.AGUARDANDO_APROVACAO)
            return Forbid();
        return Ok(Format(await u.ChangeStatus(id, x.Status)));
    }

    [HttpGet("metrics/average-execution-hours")]
    [Authorize(Roles = "Atendente,Administrador")]
    public async Task<IActionResult> Metrics() => Ok(await u.ExecutionAverage());

    [HttpPost("{id:guid}/tracking-link")]
    [Authorize(Roles = "Atendente,Administrador")]
    public async Task<IActionResult> TrackingLink(Guid id, [FromServices] OficinaBrasil.API.Services.TrackingAccess access)
    {
        if (await d.Orders.Get(id) == null) return NotFound();
        Response.Headers.CacheControl = "no-store";
        var token = access.Issue(id);
        return Ok(new { token, path = $"/api/tracking/{id}?token={Uri.EscapeDataString(token)}", expiresInDays = 30 });
    }
    internal static object Format(ServiceOrder o) => new
    {
        Estimate = o.Estimate == null ? null : new
        {
            o.Estimate.Id, o.Estimate.Diagnosis, o.Estimate.Summary, o.Estimate.Total,
            o.Estimate.CreatedAt, o.Estimate.ExpiresAt, o.Estimate.Approved,
            o.Estimate.DecidedAt, o.Estimate.EmailSentAt
        },
        o.Id, o.VehicleId, o.Notes, Status = o.Status.ToString(), o.CreatedAt, o.StartedAt, o.FinishedAt, o.DeliveredAt, o.Total, Services = o.Services.Select(s => new
        {
            s.WorkshopServiceId, s.UnitPrice
        }), Parts = o.Parts.Select(p => new
        {
            p.PartId, p.Quantity, p.UnitPrice
        })
    };
}
public class StatusInput
{
    public ServiceOrderStatus Status { get; set; }
    public StatusInput()
    {
    }

    public StatusInput(ServiceOrderStatus status)
    {
        Status = status;
    }
}
