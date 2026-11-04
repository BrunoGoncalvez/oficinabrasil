using Microsoft.EntityFrameworkCore;
using OficinaBrasil.Domain;
namespace OficinaBrasil.Infrastructure;

public class WorkshopDbContext : DbContext
{
    public WorkshopDbContext(DbContextOptions<WorkshopDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<User> Users => Set<User>();
    public DbSet<WorkshopService> Services => Set<WorkshopService>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<ServiceOrder> ServiceOrders => Set<ServiceOrder>();
    public DbSet<OrderServiceItem> OrderServiceItems => Set<OrderServiceItem>();
    public DbSet<OrderPartItem> OrderPartItems => Set<OrderPartItem>();
    public DbSet<Estimate> Estimates => Set<Estimate>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        // Item IDs are assigned by the domain. New items added to a tracked OS
        // must be inserted, rather than mistaken for existing rows by EF.
        b.Entity<OrderServiceItem>().Property(x => x.Id).ValueGeneratedNever();
        b.Entity<OrderPartItem>().Property(x => x.Id).ValueGeneratedNever();
        b.Entity<Estimate>().HasIndex(x => x.ServiceOrderId).IsUnique();
        b.Entity<ServiceOrder>().HasOne(x => x.Estimate).WithOne().HasForeignKey<Estimate>(x => x.ServiceOrderId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Estimate>().Property(x => x.Total).HasPrecision(12, 2);
        b.Entity<Estimate>().Property(x => x.TokenHash).HasMaxLength(64);
        b.Entity<Estimate>().Property(x => x.CustomerEmail).HasMaxLength(254);
        b.Entity<Estimate>().Property(x => x.Version).IsConcurrencyToken();
        // SaveChanges uses the original balance/status in the UPDATE predicate.
        // A competing stock movement or OS transition rolls back the entire save.
        b.Entity<Part>().Property(x => x.Quantity).IsConcurrencyToken();
        b.Entity<ServiceOrder>().Property(x => x.Status).IsConcurrencyToken();
        b.Entity<Customer>().HasIndex(x => x.Document).IsUnique();
        b.Entity<Customer>().Property(x => x.Document).HasMaxLength(14);
        b.Entity<Vehicle>().HasIndex(x => x.Plate).IsUnique();
        b.Entity<Vehicle>().Property(x => x.Plate).HasMaxLength(7);
        b.Entity<Vehicle>().HasOne(x => x.Customer).WithMany(c => c.Vehicles).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<User>().Property(x => x.Role).HasConversion<string>();
        b.Entity<Part>().HasIndex(x => x.Sku).IsUnique();
        b.Entity<WorkshopService>().Property(x => x.Price).HasPrecision(12, 2);
        b.Entity<Part>().Property(x => x.UnitPrice).HasPrecision(12, 2);
        b.Entity<OrderServiceItem>().Property(x => x.UnitPrice).HasPrecision(12, 2);
        b.Entity<OrderPartItem>().Property(x => x.UnitPrice).HasPrecision(12, 2);
        b.Entity<ServiceOrder>().Property(x => x.Status).HasConversion<int>();
        b.Entity<ServiceOrder>().Ignore(x => x.Total);
        b.Entity<ServiceOrder>().HasOne(x => x.Vehicle).WithMany(x => x.Orders).HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ServiceOrder>().HasMany(x => x.Services).WithOne().HasForeignKey(x => x.ServiceOrderId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ServiceOrder>().HasMany(x => x.Parts).WithOne().HasForeignKey(x => x.ServiceOrderId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<OrderPartItem>().HasOne(x => x.Part).WithMany().HasForeignKey(x => x.PartId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<OrderServiceItem>().HasOne(x => x.WorkshopService).WithMany().HasForeignKey(x => x.WorkshopServiceId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<StockMovement>().HasOne(x => x.Part).WithMany().HasForeignKey(x => x.PartId).OnDelete(DeleteBehavior.Restrict);
    }
}
