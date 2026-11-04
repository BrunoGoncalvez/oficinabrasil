using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OficinaBrasil.Application;
using OficinaBrasil.Domain;
using OficinaBrasil.Infrastructure;
using OficinaBrasil.API.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace OficinaBrasil.Domain.Tests;

[Collection("mysql")]
public class ApiIntegrationTests(MySqlApiFixture app)
{
    private static int nextPlate = 1000;
    private async Task<HttpClient> Login(string? email = null, string? password = null)
    {
        var client = app.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = email ?? app.AdminEmail, password = password ?? app.AdminPassword });
        response.EnsureSuccessStatusCode();
        var body = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body["accessToken"]!.GetValue<string>());
        return client;
    }

    private static async Task<Guid> Create(HttpClient client, string route, object input)
    {
        var response = await client.PostAsJsonAsync(route, input);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<JsonObject>())!["id"]!.GetValue<Guid>();
    }

    private async Task<(HttpClient Client, Guid Order, Guid Customer, Guid Vehicle, Guid Service, Guid Part)> Scenario(int quantity = 5)
    {
        var client = await Login();
        // Generate a distinct valid CPF without relying on real identities.
        var prefix = Random.Shared.Next(100000000, 999999999).ToString();
        var cpf = prefix;
        for (var n = 9; n <= 10; n++) { var d = cpf.Select((c, i) => (c - '0') * (n + 1 - i)).Sum() * 10 % 11; cpf += (d == 10 ? 0 : d).ToString(); }
        var customer = await Create(client, "/api/customers", new { name = "Cliente integração", document = cpf, email = "integration@example.test" });
        var plate = "TST" + Interlocked.Increment(ref nextPlate);
        var vehicle = await Create(client, "/api/vehicles", new { customerId = customer, plate, brand = "Fiat", model = "Argo", year = 2020 });
        var service = await Create(client, "/api/services", new { name = "Freios", price = 180, active = true });
        var part = await Create(client, "/api/parts", new { name = "Pastilhas", sku = Guid.NewGuid().ToString("N"), unitPrice = 220, quantity });
        var order = await Create(client, "/api/orders", new { vehicleId = vehicle, notes = "Ruído ao frear", services = new[] { new { serviceId = service } }, parts = new[] { new { partId = part, quantity = 1 } } });
        return (client, order, customer, vehicle, service, part);
    }

    private async Task<(Guid Id, string Token)> Estimate(HttpClient client, Guid order, Guid service, Guid part)
    {
        (await client.PatchAsJsonAsync($"/api/orders/{order}/status", new { status = 1 })).EnsureSuccessStatusCode();
        var id = await Create(client, $"/api/orders/{order}/estimate", new { diagnosis = "Desgaste", services = new[] { new { serviceId = service } }, parts = new[] { new { partId = part, quantity = 1 } } });
        using var scope = app.Services.CreateScope();
        var estimate = await scope.ServiceProvider.GetRequiredService<WorkshopDbContext>().Estimates.AsNoTracking().SingleAsync(x => x.Id == id);
        return (id, Regex.Match(estimate.EmailBody, "token=([A-F0-9]+)").Groups[1].Value);
    }

    private static Task<HttpResponseMessage> Decide(HttpClient client, Guid id, string token, bool approve) =>
        client.PostAsync($"/api/estimates/{id}/decision", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["token"] = token, ["approve"] = approve.ToString() }));

    [Fact]
    public async Task CompleteOrderThroughHttpAndMySql()
    {
        var s = await Scenario();
        var estimate = await Estimate(s.Client, s.Order, s.Service, s.Part);
        var publicClient = app.CreateClient();
        (await publicClient.GetAsync($"/api/estimates/{estimate.Id}/decision?token={estimate.Token}&approve=true")).EnsureSuccessStatusCode();
        using (var scope = app.Services.CreateScope())
            Assert.Equal(ServiceOrderStatus.AGUARDANDO_APROVACAO, (await scope.ServiceProvider.GetRequiredService<WorkshopDbContext>().ServiceOrders.FindAsync(s.Order))!.Status);
        (await Decide(publicClient, estimate.Id, estimate.Token, true)).EnsureSuccessStatusCode();
        foreach (var status in new[] { 3, 4, 5 })
            (await s.Client.PatchAsJsonAsync($"/api/orders/{s.Order}/status", new { status })).EnsureSuccessStatusCode();
        var body = (await s.Client.GetFromJsonAsync<JsonObject>($"/api/orders/{s.Order}"))!;
        Assert.Equal("ENTREGUE", body["status"]!.GetValue<string>());
        Assert.Equal(400m, body["total"]!.GetValue<decimal>());
        Assert.DoesNotContain("tokenHash", body.ToJsonString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("emailBody", body.ToJsonString(), StringComparison.OrdinalIgnoreCase);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WorkshopDbContext>();
            Assert.Equal(4, (await db.Parts.FindAsync(s.Part))!.Quantity);
            Assert.Single(await db.StockMovements.Where(x => x.PartId == s.Part && x.Delta < 0).ToListAsync());
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await Decide(publicClient, estimate.Id, estimate.Token, false)).StatusCode);
        (await s.Client.GetAsync("/api/orders/metrics/average-execution-hours")).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task RejectionCancelsWithoutConsumingStock()
    {
        var s = await Scenario();
        var estimate = await Estimate(s.Client, s.Order, s.Service, s.Part);
        (await Decide(app.CreateClient(), estimate.Id, estimate.Token, false)).EnsureSuccessStatusCode();
        var body = (await s.Client.GetFromJsonAsync<JsonObject>($"/api/orders/{s.Order}"))!;
        Assert.Equal("CANCELADA", body["status"]!.GetValue<string>());
        using var scope = app.Services.CreateScope();
        Assert.Equal(5, (await scope.ServiceProvider.GetRequiredService<WorkshopDbContext>().Parts.FindAsync(s.Part))!.Quantity);
    }

    [Fact]
    public async Task ExpiredBudgetCanBeReissuedAndOldLinkIsRevoked()
    {
        var s = await Scenario();
        var estimate = await Estimate(s.Client, s.Order, s.Service, s.Part);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WorkshopDbContext>();
            (await db.Estimates.FindAsync(estimate.Id))!.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await Decide(app.CreateClient(), estimate.Id, estimate.Token, true)).StatusCode);
        (await s.Client.PostAsync($"/api/orders/{s.Order}/estimate/reissue", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.BadRequest, (await Decide(app.CreateClient(), estimate.Id, estimate.Token, true)).StatusCode);
        using var scope2 = app.Services.CreateScope();
        var renewed = (await scope2.ServiceProvider.GetRequiredService<WorkshopDbContext>().Estimates.FindAsync(estimate.Id))!;
        var token = Regex.Match(renewed.EmailBody, "token=([A-F0-9]+)").Groups[1].Value;
        (await Decide(app.CreateClient(), estimate.Id, token, true)).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task InsufficientStockRollsBackOrderAndMovements()
    {
        var s = await Scenario(0);
        var estimate = await Estimate(s.Client, s.Order, s.Service, s.Part);
        (await Decide(app.CreateClient(), estimate.Id, estimate.Token, true)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.BadRequest, (await s.Client.PatchAsJsonAsync($"/api/orders/{s.Order}/status", new { status = 3 })).StatusCode);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkshopDbContext>();
        Assert.Equal(ServiceOrderStatus.APROVADA, (await db.ServiceOrders.FindAsync(s.Order))!.Status);
        Assert.Empty(await db.StockMovements.Where(x => x.PartId == s.Part).ToListAsync());
    }

    [Fact]
    public async Task ConcurrentStockUpdatesRejectStaleBalanceAndRollBackMovement()
    {
        var s = await Scenario(1);
        using var scope1 = app.Services.CreateScope();
        using var scope2 = app.Services.CreateScope();
        var db1 = scope1.ServiceProvider.GetRequiredService<WorkshopDbContext>();
        var db2 = scope2.ServiceProvider.GetRequiredService<WorkshopDbContext>();
        var first = (await db1.Parts.FindAsync(s.Part))!;
        var stale = (await db2.Parts.FindAsync(s.Part))!;
        first.AdjustStock(-1);
        stale.AdjustStock(-1);
        db1.StockMovements.Add(new StockMovement { PartId = s.Part, Delta = -1, Reason = "first" });
        db2.StockMovements.Add(new StockMovement { PartId = s.Part, Delta = -1, Reason = "stale" });
        await db1.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
        using var verification = app.Services.CreateScope();
        var db = verification.ServiceProvider.GetRequiredService<WorkshopDbContext>();
        Assert.Equal(0, (await db.Parts.FindAsync(s.Part))!.Quantity);
        Assert.Single(await db.StockMovements.Where(x => x.PartId == s.Part && x.Delta < 0).ToListAsync());
    }

    [Fact]
    public async Task ConcurrentExecutionRollsBackTheLosingOrder()
    {
        var s = await Scenario(1);
        var firstEstimate = await Estimate(s.Client, s.Order, s.Service, s.Part);
        (await Decide(app.CreateClient(), firstEstimate.Id, firstEstimate.Token, true)).EnsureSuccessStatusCode();
        var secondOrder = await Create(s.Client, "/api/orders", new { vehicleId = s.Vehicle });
        var secondEstimate = await Estimate(s.Client, secondOrder, s.Service, s.Part);
        (await Decide(app.CreateClient(), secondEstimate.Id, secondEstimate.Token, true)).EnsureSuccessStatusCode();
        using var scope1 = app.Services.CreateScope();
        using var scope2 = app.Services.CreateScope();
        var db1 = scope1.ServiceProvider.GetRequiredService<WorkshopDbContext>();
        var db2 = scope2.ServiceProvider.GetRequiredService<WorkshopDbContext>();
        var data1 = new WorkshopData(db1);
        var data2 = new WorkshopData(db2);
        // Both units of work read the same balance before either saves.
        await data1.GetOrderWithItems(s.Order);
        await data2.GetOrderWithItems(secondOrder);
        await data1.Parts.Get(s.Part);
        await data2.Parts.Get(s.Part);
        await new ServiceOrderUseCases(data1).ChangeStatus(s.Order, ServiceOrderStatus.EM_EXECUCAO);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => new ServiceOrderUseCases(data2).ChangeStatus(secondOrder, ServiceOrderStatus.EM_EXECUCAO));
        using var scope3 = app.Services.CreateScope();
        var db = scope3.ServiceProvider.GetRequiredService<WorkshopDbContext>();
        Assert.Equal(ServiceOrderStatus.APROVADA, (await db.ServiceOrders.FindAsync(secondOrder))!.Status);
        Assert.Equal(ServiceOrderStatus.EM_EXECUCAO, (await db.ServiceOrders.FindAsync(s.Order))!.Status);
        Assert.Equal(0, (await db.Parts.FindAsync(s.Part))!.Quantity);
        Assert.Single(await db.StockMovements.Where(x => x.PartId == s.Part && x.Delta < 0).ToListAsync());
    }

    [Fact]
    public async Task AdministrativeRoutesRequireJwtAndMechanicHasReadOnlyCatalogs()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await app.CreateClient().GetAsync("/api/customers")).StatusCode);
        var admin = await Login();
        var email = Guid.NewGuid() + "@example.test";
        var password = Guid.NewGuid() + "Aa1!";
        (await admin.PostAsJsonAsync("/api/auth/employees", new { name = "Mecânico", email, password, role = 1 })).EnsureSuccessStatusCode();
        var mechanic = await Login(email, password);
        (await mechanic.GetAsync("/api/parts")).EnsureSuccessStatusCode();
        (await mechanic.GetAsync("/api/services")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, (await mechanic.PostAsJsonAsync("/api/services", new { name = "Negado", price = 10 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await mechanic.GetAsync("/api/customers")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await app.CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password = "wrong" })).StatusCode);
    }

    [Fact]
    public async Task CustomerLookupNormalizesDocumentsAndRejectsInvalidCnpj()
    {
        var admin = await Login();
        var customer = await Create(admin, "/api/customers", new { name = "Empresa de teste", document = "11.222.333/0001-81", email = "empresa@example.test" });
        var lookup = await admin.GetFromJsonAsync<JsonObject>("/api/customers/by-document?document=11.222.333%2F0001-81");
        Assert.Equal(customer, lookup!["id"]!.GetValue<Guid>());
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/customers/by-document?document=00000000000000")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/customers/by-document")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/customers/by-document?document=52998224725")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/customers", new { name = "Duplicado", document = "11222333000181" })).StatusCode);
    }

    [Fact]
    public async Task CrudUpdatesAndDeletesPersistInMySql()
    {
        var s = await Scenario();
        var service = await Create(s.Client, "/api/services", new { name = "Livre", price = 10 });
        (await s.Client.PutAsJsonAsync($"/api/services/{service}", new { name = "Atualizado", price = 20 })).EnsureSuccessStatusCode();
        Assert.Equal(20m, (await s.Client.GetFromJsonAsync<JsonObject>($"/api/services/{service}"))!["price"]!.GetValue<decimal>());
        (await s.Client.DeleteAsync($"/api/services/{service}")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await s.Client.GetAsync($"/api/services/{service}")).StatusCode);
        (await s.Client.PutAsJsonAsync($"/api/parts/{s.Part}", new { name = "Pastilha atualizada", sku = "UPD-" + s.Part, unitPrice = 230 })).EnsureSuccessStatusCode();
        (await s.Client.PostAsJsonAsync($"/api/parts/{s.Part}/stock", new { delta = 2, reason = "Reposição" })).EnsureSuccessStatusCode();
        Assert.Equal(7, (await s.Client.GetFromJsonAsync<JsonObject>($"/api/parts/{s.Part}"))!["quantity"]!.GetValue<int>());
        (await s.Client.GetAsync("/api/orders")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await s.Client.DeleteAsync($"/api/vehicles/{s.Vehicle}")).StatusCode);
    }

    [Fact]
    public async Task TrackingTokenGrantsAccessOnlyToItsOrder()
    {
        var s = await Scenario();
        var response = await s.Client.PostAsync($"/api/orders/{s.Order}/tracking-link", null);
        response.EnsureSuccessStatusCode();
        var link = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        var token = link["token"]!.GetValue<string>();
        var publicClient = app.CreateClient();
        var tracking = await publicClient.GetAsync(link["path"]!.GetValue<string>());
        tracking.EnsureSuccessStatusCode();
        var body = await tracking.Content.ReadAsStringAsync();
        Assert.Contains("RECEBIDA", body);
        Assert.DoesNotContain("document", body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.NotFound, (await publicClient.GetAsync($"/api/tracking/{Guid.NewGuid()}?token={Uri.EscapeDataString(token)}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await publicClient.GetAsync($"/api/tracking/{s.Order}?token=invalid")).StatusCode);
    }

    [Fact]
    public async Task DemoSeedCreatesConsistentScenariosAndIsIdempotent()
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkshopDbContext>();
        await DemoSeeder.Run(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<IConfiguration>());
        var marker = Guid.Parse("de000000-0000-0000-0004-000000000001");
        Assert.Equal(ServiceOrderStatus.RECEBIDA, (await db.ServiceOrders.FindAsync(marker))!.Status);
        var seedOrders = await db.ServiceOrders.Where(x => x.Notes != null && x.Notes.StartsWith("[DEMO]")).ToListAsync();
        Assert.Equal(8, seedOrders.Count);
        Assert.Equal(8, seedOrders.Select(x => x.Status).Distinct().Count());
        var partId = Guid.Parse("de000000-0000-0000-0005-000000000003");
        Assert.Equal(7, (await db.Parts.FindAsync(partId))!.Quantity);
        Assert.Equal(3, await db.StockMovements.CountAsync(x => x.PartId == partId && x.Delta == -1));
        var counts = new[] { await db.Customers.CountAsync(), await db.ServiceOrders.CountAsync(), await db.Estimates.CountAsync(), await db.StockMovements.CountAsync() };
        await DemoSeeder.Run(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<IConfiguration>());
        Assert.Equal(counts, new[] { await db.Customers.CountAsync(), await db.ServiceOrders.CountAsync(), await db.Estimates.CountAsync(), await db.StockMovements.CountAsync() });
    }

    [Fact]
    public async Task EmailWorkerDeliversToSmtpAndRecordsDelivery()
    {
        var s = await Scenario();
        var estimate = await Estimate(s.Client, s.Order, s.Service, s.Part);
        await using var smtp = new SmtpCapture();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "127.0.0.1", ["Smtp:Port"] = smtp.Port.ToString(),
            ["Smtp:EnableSsl"] = "false", ["Smtp:From"] = "oficina@example.test"
        }).Build();
        using var worker = new EstimateEmailWorker(app.Services.GetRequiredService<IServiceScopeFactory>(), config, NullLogger<EstimateEmailWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try
        {
            while (true)
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<WorkshopDbContext>();
                if ((await db.Estimates.AsNoTracking().SingleAsync(x => x.Id == estimate.Id)).EmailSentAt.HasValue) break;
                await Task.Delay(50, timeout.Token);
            }
        }
        finally { await worker.StopAsync(CancellationToken.None); }
        Assert.NotEmpty(smtp.Messages);
        Assert.Contains(smtp.Messages, message =>
        {
            var sections = new Regex("\r?\n\r?\n").Split(message, 2);
            var body = sections.Length == 2 ? sections[1] : "";
            if (sections[0].Contains("Content-Transfer-Encoding: base64", StringComparison.OrdinalIgnoreCase))
                body = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(body));
            else
                body = Regex.Replace(body, @"=\r?\n", "");
            return body.Contains(s.Order.ToString()) && message.Contains("integration@example.test");
        });
    }
}
