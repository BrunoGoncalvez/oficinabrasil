using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OficinaBrasil.Application;
using OficinaBrasil.Infrastructure;
using OficinaBrasil.Domain;
var builder = WebApplication.CreateBuilder(args);
var conn = builder.Configuration.GetConnectionString("MySql") ?? throw new Exception("Configure ConnectionStrings:MySql");
if (string.IsNullOrWhiteSpace(conn)) throw new Exception("Configure ConnectionStrings:MySql");
builder.Services.AddDbContext<WorkshopDbContext>(o => o.UseMySql(conn, ServerVersion.Parse("8.0.36-mysql")));
builder.Services.AddScoped<IWorkshopData, WorkshopData>();
builder.Services.AddScoped<CustomerUseCases>();
builder.Services.AddScoped<VehicleUseCases>();
builder.Services.AddScoped<ServiceCatalogUseCases>();
builder.Services.AddScoped<InventoryUseCases>();
builder.Services.AddScoped<ServiceOrderUseCases>();
builder.Services.AddSingleton(builder.Configuration.GetSection("Estimates").Get<EstimateSettings>() ?? new EstimateSettings());
builder.Services.AddScoped<EstimateUseCases>();
var protection = builder.Services.AddDataProtection().SetApplicationName("OficinaBrasil");
var protectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(protectionKeysPath))
    protection.PersistKeysToFileSystem(new DirectoryInfo(protectionKeysPath));
builder.Services.AddSingleton<OficinaBrasil.API.Services.TrackingAccess>();
builder.Services.AddHostedService<OficinaBrasil.API.Services.EstimateEmailWorker>();
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT"
    }); o.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        [new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Reference = new Microsoft.OpenApi.Models.OpenApiReference
            {
                Id = "Bearer", Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme
            }
        }] = Array.Empty<string>()
    });
});
var key = builder.Configuration["Jwt:Key"] ?? throw new Exception("Configure Jwt:Key");
if (Encoding.UTF8.GetByteCount(key)< 32)
    throw new Exception("Jwt:Key precisa ter ao menos 32 bytes");
if (!builder.Environment.IsDevelopment() && key.StartsWith("TROQUE", StringComparison.OrdinalIgnoreCase))
    throw new Exception("Configure uma Jwt:Key secreta fora do ambiente de desenvolvimento");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).Configure<IConfiguration>((options, configuration) => options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true, ValidIssuer = configuration["Jwt:Issuer"], ValidateAudience = true, ValidAudience = configuration["Jwt:Audience"], ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)), ValidateLifetime = true, ClockSkew = TimeSpan.FromMinutes(1)
});
builder.Services.AddAuthorization();
var app = builder.Build();
app.Use(async(ctx, next) =>
{
    try
    {
        await next();
    }
    catch (BusinessException e)
    {
        ctx.Response.StatusCode = 400; await ctx.Response.WriteAsJsonAsync(new
        {
            error = e.Message
        });
    }
    catch (KeyNotFoundException e)
    {
        ctx.Response.StatusCode = 404; await ctx.Response.WriteAsJsonAsync(new
        {
            error = e.Message
        });
    }
    catch (DbUpdateConcurrencyException)
    {
        ctx.Response.StatusCode = 409;
        await ctx.Response.WriteAsJsonAsync(new { error = "Os dados foram alterados por outra operação. Consulte novamente antes de tentar." });
    }
    catch (DbUpdateException)
    {
        ctx.Response.StatusCode = 409; await ctx.Response.WriteAsJsonAsync(new
        {
            error = "Conflito ao gravar dados"
        });
    }
});
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WorkshopDbContext>();
    if (args.Contains("--seed-demo") && !app.Environment.IsDevelopment())
        throw new InvalidOperationException("A seed demo só pode executar em Development");
    await db.Database.EnsureCreatedAsync();
    if (args.Contains("--seed-demo"))
    {
        if (!app.Environment.IsDevelopment())
            throw new InvalidOperationException("A seed demo só pode executar em Development");
        await OficinaBrasil.API.Services.DemoSeeder.Run(scope.ServiceProvider, app.Configuration);
        return;
    }
    var email = app.Configuration["SeedAdmin:Email"];
    var password = app.Configuration["SeedAdmin:Password"];
    if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password) && !await db.Users.AnyAsync())
    {
        if (password.Length< 8)
            throw new Exception("SeedAdmin:Password requer ao menos 8 caracteres");
        var admin = new User
        {
            Name = "Administrador", Email = email.ToLowerInvariant(), Role = EmployeeRole.Administrador
        };
        admin.PasswordHash = new Microsoft.AspNetCore.Identity.PasswordHasher<User>().HashPassword(admin, password);
        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }
}
app.Run();
public partial class Program
{
}
