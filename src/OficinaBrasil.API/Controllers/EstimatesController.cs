using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaBrasil.Application;

namespace OficinaBrasil.API.Controllers;

[ApiController]
public class EstimatesController(EstimateUseCases useCases) : ControllerBase
{
    [HttpPost("api/orders/{orderId:guid}/estimate/reissue")]
    [Authorize(Roles = "Atendente,Mecanico,Administrador")]
    public async Task<IActionResult> Reissue(Guid orderId)
    {
        var estimate = await useCases.Reissue(orderId);
        return Ok(new { estimate.Id, estimate.ExpiresAt, EmailStatus = "PENDENTE" });
    }
    [HttpPost("api/orders/{orderId:guid}/estimate")]
    [Authorize(Roles = "Mecanico,Administrador")]
    public async Task<IActionResult> Create(Guid orderId, EstimateInput input)
    {
        var estimate = await useCases.Create(orderId, input);
        return StatusCode(201, new { estimate.Id, estimate.ServiceOrderId, estimate.Diagnosis, estimate.Summary,
            estimate.Total, estimate.CreatedAt, estimate.ExpiresAt, EmailStatus = "PENDENTE" });
    }

    [HttpGet("api/estimates/{id:guid}/decision")]
    [AllowAnonymous]
    public async Task<IActionResult> Confirmation(Guid id, [FromQuery] string token, [FromQuery, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] bool approve)
    {
        PrivateResponse();
        var estimate = await useCases.GetForDecision(id, token);
        var decision = approve ? "aprovação" : "reprovação";
        var body = estimate.Approved.HasValue
            ? "<p>Este orçamento já foi respondido.</p>"
            : $"<form method=\"post\" action=\"/api/estimates/{id}/decision\">" +
              $"<input type=\"hidden\" name=\"token\" value=\"{WebUtility.HtmlEncode(token)}\">" +
              $"<input type=\"hidden\" name=\"approve\" value=\"{approve.ToString().ToLowerInvariant()}\">" +
              $"<button type=\"submit\">Confirmar {decision}</button></form>";
        return Content("<!doctype html><html lang=\"pt-BR\"><meta charset=\"utf-8\">" +
            "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>Confirmar orçamento</title>" +
            $"<body><h1>Orçamento da OS {estimate.ServiceOrderId}</h1><p>{WebUtility.HtmlEncode(estimate.Diagnosis)}</p>" +
            $"<pre>{WebUtility.HtmlEncode(estimate.Summary)}</pre><p>Total: {estimate.Total.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"))}</p>{body}</body></html>", "text/html; charset=utf-8");
    }

    [HttpPost("api/estimates/{id:guid}/decision")]
    [AllowAnonymous]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Decide(Guid id, [FromForm] string token, [FromForm, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] bool approve)
    {
        PrivateResponse();
        await useCases.Decide(id, token, approve);
        return Content(approve ? "Orçamento aprovado. Obrigado!" : "Orçamento reprovado. A OS foi cancelada.", "text/plain; charset=utf-8");
    }

    private void PrivateResponse()
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Content-Security-Policy"] = "default-src 'none'; form-action 'self'; frame-ancestors 'none'; base-uri 'none'";
    }
}
