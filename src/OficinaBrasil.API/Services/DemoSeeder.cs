using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OficinaBrasil.Application;
using OficinaBrasil.Domain;
using OficinaBrasil.Infrastructure;

namespace OficinaBrasil.API.Services;

public static class DemoSeeder
{
    private static Guid DemoId(int group, int number) => Guid.Parse($"de000000-0000-0000-{group:0000}-{number:000000000000}");

    public static async Task Run(IServiceProvider services, IConfiguration configuration)
    {
        var db = services.GetRequiredService<WorkshopDbContext>();
        // EnsureCreated does not update an existing database. This additive script is idempotent.
        using (var stream = typeof(DemoSeeder).Assembly.GetManifestResourceStream("Demo.Estimates.sql")!)
        using (var reader = new StreamReader(stream))
            await db.Database.ExecuteSqlRawAsync(await reader.ReadToEndAsync());

        await using var transaction = await db.Database.BeginTransactionAsync();
        if (await db.ServiceOrders.AnyAsync(x => x.Id == DemoId(4, 1)))
        {
            Console.WriteLine("Massa de demonstração já cadastrada. Dados e decisões existentes foram preservados.");
            return;
        }

        var password = configuration["SeedDemo:Password"] ?? "Demo@12345";
        if (password.Length < 8) throw new InvalidOperationException("SeedDemo:Password requer ao menos 8 caracteres");
        var hasher = new PasswordHasher<User>();
        foreach (var (name, email, role) in new[]
        {
            ("Ana Souza (demo)", "atendente@oficina.demo", EmployeeRole.Atendente),
            ("Carlos Lima (demo)", "mecanico@oficina.demo", EmployeeRole.Mecanico),
            ("Paulo Santos (demo)", "estoquista@oficina.demo", EmployeeRole.Estoquista),
            ("Administrador (demo)", "admin@oficina.demo", EmployeeRole.Administrador)
        })
        {
            if (await db.Users.AnyAsync(x => x.Email == email)) continue;
            var user = new User { Name = name, Email = email, Role = role };
            user.PasswordHash = hasher.HashPassword(user, password);
            db.Users.Add(user);
        }

        var customerNames = new[] { "Mariana Oliveira", "Rafael Almeida", "Beatriz Costa", "Lucas Ferreira" };
        var customers = new List<Customer>();
        for (var i = 0; i < customerNames.Length; i++)
        {
            var document = DemoCpf(i + 1);
            var customer = await db.Customers.SingleOrDefaultAsync(x => x.Document == document);
            if (customer == null)
            {
                customer = new Customer { Id = DemoId(1, i + 1), Name = customerNames[i], Document = Checks.Document(document),
                    Email = $"cliente{i + 1}@example.test", Phone = $"1195555000{i + 1}" };
                db.Customers.Add(customer);
            }
            customers.Add(customer);
        }
        var cars = new[]
        {
            ("Volkswagen", "Gol 1.6", 2019),
            ("Chevrolet", "Onix 1.0", 2021),
            ("Fiat", "Argo 1.3", 2020),
            ("Toyota", "Corolla 2.0", 2022)
        };
        var vehicles = new List<Vehicle>();
        for (var i = 0; i < cars.Length; i++)
        {
            var plate = Checks.Plate($"DMO{i + 1}A0{i + 1}");
            var vehicle = await db.Vehicles.SingleOrDefaultAsync(x => x.Plate == plate);
            if (vehicle == null)
            {
                vehicle = new Vehicle { Id = DemoId(2, i + 1), CustomerId = customers[i].Id, Plate = plate,
                    Brand = cars[i].Item1, Model = cars[i].Item2, Year = cars[i].Item3 };
                db.Vehicles.Add(vehicle);
            }
            vehicles.Add(vehicle);
        }

        var catalog = new[] { ("Troca de óleo e filtros", 120m), ("Substituição de pastilhas dianteiras", 180m),
            ("Alinhamento e balanceamento", 160m), ("Diagnóstico eletrônico", 150m) };
        var workshopServices = new List<WorkshopService>();
        for (var i = 0; i < catalog.Length; i++)
        {
            var id = DemoId(3, i + 1);
            var service = await db.Services.FindAsync(id);
            if (service == null)
            {
                service = new WorkshopService { Id = id, Name = catalog[i].Item1, Price = catalog[i].Item2, Active = true };
                db.Services.Add(service);
            }
            workshopServices.Add(service);
        }

        var stock = new[] { ("Óleo sintético 5W30 (1L)", 45m, 40), ("Filtro de óleo", 35m, 20),
            ("Jogo de pastilhas dianteiras", 220m, 10), ("Filtro de ar", 55m, 0) };
        var parts = new List<Part>();
        for (var i = 0; i < stock.Length; i++)
        {
            var sku = $"DEMO-{i + 1:000}";
            var part = await db.Parts.SingleOrDefaultAsync(x => x.Sku == sku);
            if (part == null)
            {
                part = new Part { Id = DemoId(5, i + 1), Sku = sku, Name = stock[i].Item1,
                    UnitPrice = stock[i].Item2, Quantity = stock[i].Item3, Description = "Catálogo de demonstração" };
                db.Parts.Add(part);
                if (part.Quantity > 0) db.StockMovements.Add(new StockMovement
                    { PartId = part.Id, Delta = part.Quantity, Reason = "Saldo inicial — demonstração" });
            }
            parts.Add(part);
        }
        await db.SaveChangesAsync();

        var targets = new[] { ServiceOrderStatus.RECEBIDA, ServiceOrderStatus.EM_DIAGNOSTICO,
            ServiceOrderStatus.AGUARDANDO_APROVACAO, ServiceOrderStatus.APROVADA, ServiceOrderStatus.EM_EXECUCAO,
            ServiceOrderStatus.FINALIZADA, ServiceOrderStatus.ENTREGUE, ServiceOrderStatus.CANCELADA };
        var estimates = services.GetRequiredService<EstimateUseCases>();
        var orders = services.GetRequiredService<ServiceOrderUseCases>();
        for (var i = 0; i < targets.Length; i++)
        {
            var target = targets[i];
            var order = new ServiceOrder { Id = DemoId(4, i + 1), VehicleId = vehicles[i % vehicles.Count].Id,
                CreatedAt = DateTime.UtcNow.AddDays(-i), Notes = $"[DEMO] Cenário inicial: {target}. Cliente relata ruído nos freios dianteiros." };
            db.ServiceOrders.Add(order);
            await db.SaveChangesAsync();
            if (target == ServiceOrderStatus.RECEBIDA) continue;
            await orders.ChangeStatus(order.Id, ServiceOrderStatus.EM_DIAGNOSTICO);
            if (target == ServiceOrderStatus.EM_DIAGNOSTICO) continue;
            var estimate = await estimates.Create(order.Id, new EstimateInput
            {
                Diagnosis = "Pastilhas dianteiras desgastadas. Recomendada substituição do jogo e inspeção dos discos.",
                Services = new() { new(workshopServices[1].Id) }, Parts = new() { new(parts[2].Id, 1) }
            });
            if (target == ServiceOrderStatus.AGUARDANDO_APROVACAO) continue;
            var token = Regex.Match(estimate.EmailBody, "token=([A-F0-9]+)").Groups[1].Value;
            await estimates.Decide(estimate.Id, token, target != ServiceOrderStatus.CANCELADA);
            // Historical scenarios have already completed their notification step.
            estimate.CreatedAt = order.CreatedAt.AddMinutes(30);
            estimate.ExpiresAt = estimate.CreatedAt.AddHours(72);
            db.Entry(estimate).Property(x => x.DecidedAt).CurrentValue = estimate.CreatedAt.AddMinutes(30);
            estimate.EmailSentAt = estimate.CreatedAt;
            if (target is ServiceOrderStatus.EM_EXECUCAO or ServiceOrderStatus.FINALIZADA or ServiceOrderStatus.ENTREGUE)
            {
                await orders.ChangeStatus(order.Id, ServiceOrderStatus.EM_EXECUCAO);
                db.Entry(order).Property(x => x.StartedAt).CurrentValue = DateTime.UtcNow.AddHours(-i - 2);
            }
            if (target is ServiceOrderStatus.FINALIZADA or ServiceOrderStatus.ENTREGUE)
            {
                await orders.ChangeStatus(order.Id, ServiceOrderStatus.FINALIZADA);
                db.Entry(order).Property(x => x.FinishedAt).CurrentValue = DateTime.UtcNow.AddHours(-i);
            }
            if (target == ServiceOrderStatus.ENTREGUE)
            {
                await orders.ChangeStatus(order.Id, ServiceOrderStatus.ENTREGUE);
                db.Entry(order).Property(x => x.DeliveredAt).CurrentValue = DateTime.UtcNow.AddHours(-i + 1);
            }
            await db.SaveChangesAsync();
        }
        await transaction.CommitAsync();
        Console.WriteLine("Seed concluída: 4 clientes, 4 veículos, 4 serviços, 4 peças, 4 funcionários demo e 8 OS. Consulte docs/DEMO.md.");
    }

    private static string DemoCpf(int number)
    {
        var value = $"900000{number:000}";
        for (var n = 9; n <= 10; n++)
        {
            var sum = value.Select((c, index) => (c - '0') * (n + 1 - index)).Sum();
            var digit = sum * 10 % 11;
            value += (digit == 10 ? 0 : digit).ToString();
        }
        return value;
    }
}
