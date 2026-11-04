using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MySqlConnector;
using OficinaBrasil.API.Services;
using OficinaBrasil.Infrastructure;
using Xunit;

namespace OficinaBrasil.Domain.Tests;

// Real HTTP pipeline + the production MySQL provider, in an isolated disposable database.
public class MySqlApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public string AdminEmail { get; } = "integration@example.test";
    public string AdminPassword { get; } = Guid.NewGuid() + "Aa1!";
    public string ConnectionString { get; private set; } = "";
    private readonly string database = "oficina_test_" + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var source = Environment.GetEnvironmentVariable("OFICINA_TEST_MYSQL");
        if (string.IsNullOrWhiteSpace(source))
        {
            var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
            source = new ConfigurationBuilder().AddJsonFile(Path.Combine(root, "OficinaBrasil.API", "appsettings.json"))
                .AddJsonFile(Path.Combine(root, "OficinaBrasil.API", "appsettings.Development.json"), optional: true)
                .Build().GetConnectionString("MySql");
        }
        var connection = new MySqlConnectionStringBuilder(source ?? throw new InvalidOperationException("Configure OFICINA_TEST_MYSQL")) { Database = database };
        ConnectionString = connection.ConnectionString;
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:MySql"] = ConnectionString,
            ["SeedAdmin:Email"] = AdminEmail, ["SeedAdmin:Password"] = AdminPassword,
            ["Jwt:Key"] = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            ["Estimates:PublicBaseUrl"] = "https://localhost",
            ["Logging:LogLevel:Default"] = "Warning"
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<WorkshopDbContext>>();
            services.AddDbContext<WorkshopDbContext>(o => o.UseMySql(ConnectionString, ServerVersion.Parse("8.0.36-mysql")));
            foreach (var descriptor in services.Where(d => d.ImplementationType == typeof(EstimateEmailWorker)).ToList())
                services.Remove(descriptor);
        });
    }

    public Task InitializeAsync() { _ = CreateClient(); return Task.CompletedTask; }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        if (string.IsNullOrEmpty(ConnectionString)) return;
        var builder = new MySqlConnectionStringBuilder(ConnectionString) { Database = "" };
        await using var connection = new MySqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS `{database}`"; // generated name, never the configured application database
        await command.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition("mysql")]
public class MySqlCollection : ICollectionFixture<MySqlApiFixture> { }
