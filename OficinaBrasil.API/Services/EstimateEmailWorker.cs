using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using OficinaBrasil.Infrastructure;

namespace OficinaBrasil.API.Services;

public class EstimateEmailWorker(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<EstimateEmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<WorkshopDbContext>();
                var pending = await db.Estimates.Where(x => x.EmailSentAt == null && x.Approved == null && x.ExpiresAt > DateTime.UtcNow)
                    .OrderBy(x => x.CreatedAt).Take(20).ToListAsync(stoppingToken);
                foreach (var estimate in pending)
                {
                    try
                    {
                        var host = configuration["Smtp:Host"];
                        var from = configuration["Smtp:From"];
                        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
                            throw new InvalidOperationException("Configure Smtp:Host e Smtp:From para enviar os orçamentos");
                        using var client = new SmtpClient(host, configuration.GetValue("Smtp:Port", 587))
                        {
                            EnableSsl = configuration.GetValue("Smtp:EnableSsl", true),
                            UseDefaultCredentials = false
                        };
                        if (!string.IsNullOrWhiteSpace(configuration["Smtp:Username"]))
                            client.Credentials = new NetworkCredential(configuration["Smtp:Username"], configuration["Smtp:Password"]);
                        using var message = new MailMessage(from, estimate.CustomerEmail)
                        {
                            Subject = $"Orçamento da OS {estimate.ServiceOrderId}", Body = estimate.EmailBody, IsBodyHtml = true
                        };
                        await client.SendMailAsync(message, stoppingToken);
                        estimate.EmailSentAt = DateTime.UtcNow;
                        await db.SaveChangesAsync(stoppingToken);
                    }
                    catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                    {
                        logger.LogWarning("Falha ao enviar orçamento {EstimateId} ({FailureType}). Nova tentativa em 30 segundos.", estimate.Id, ex.GetType().Name);
                    }
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("Fila de e-mails indisponível ({FailureType}). Nova tentativa em 30 segundos.", ex.GetType().Name);
            }
            try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
