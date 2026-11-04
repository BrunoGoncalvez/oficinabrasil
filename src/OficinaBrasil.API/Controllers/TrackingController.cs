using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OficinaBrasil.Application;
using OficinaBrasil.Domain;
namespace OficinaBrasil.API.Controllers;

[ApiController]
[Route("api/tracking")]
[AllowAnonymous]
public class TrackingController : ControllerBase
{
    private readonly IWorkshopData d;
    private readonly OficinaBrasil.API.Services.TrackingAccess access;
    public TrackingController(IWorkshopData d, OficinaBrasil.API.Services.TrackingAccess access)
    {
        this.d = d;
        this.access = access;
    }
    // A capability token authorizes only this order, without exposing the customer's document.
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Track(Guid id, [FromQuery] string token)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        if (!access.Allows(id, token)) return NotFound();
        var o = await d.GetOrderWithItems(id);
        if (o == null)
            return NotFound();
        return Ok(new
        {
            o.Id, Status = o.Status.ToString(), o.CreatedAt, o.StartedAt, o.FinishedAt, o.DeliveredAt
        });
    }
}
