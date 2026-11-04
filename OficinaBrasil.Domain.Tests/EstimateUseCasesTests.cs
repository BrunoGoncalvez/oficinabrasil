using OficinaBrasil.Application;
using OficinaBrasil.Domain;
using Xunit;

namespace OficinaBrasil.Domain.Tests;

public class EstimateUseCasesTests
{
    internal static (MemoryData Data, ServiceOrder Order, EstimateInput Input, EstimateUseCases UseCases) Setup()
    {
        var data = new MemoryData();
        var customer = new Customer { Name = "Cliente <teste>", Email = "cliente@example.com" };
        var vehicle = new Vehicle { CustomerId = customer.Id };
        var service = new WorkshopService { Name = "Revisão", Price = 150, Active = true };
        var part = new Part { Name = "Filtro", UnitPrice = 25, Quantity = 0 };
        var order = new ServiceOrder { VehicleId = vehicle.Id };
        data.Customers.Add(customer).GetAwaiter().GetResult();
        data.Vehicles.Add(vehicle).GetAwaiter().GetResult();
        data.Services.Add(service).GetAwaiter().GetResult();
        data.Parts.Add(part).GetAwaiter().GetResult();
        data.Orders.Add(order).GetAwaiter().GetResult();
        order.Transition(ServiceOrderStatus.EM_DIAGNOSTICO);
        var input = new EstimateInput { Diagnosis = "Diagnóstico <teste>", Services = new() { new(service.Id) }, Parts = new() { new(part.Id, 2) } };
        return (data, order, input, new EstimateUseCases(data, new EstimateSettings { PublicBaseUrl = "https://oficina.example.com" }));
    }

    [Fact]
    public async Task CreatesPricedEstimateAndPendingEmailWithoutConsumingStock()
    {
        var (data, order, input, useCases) = Setup();
        var estimate = await useCases.Create(order.Id, input);
        Assert.Equal(200m, estimate.Total);
        Assert.Equal(ServiceOrderStatus.AGUARDANDO_APROVACAO, order.Status);
        Assert.Equal("cliente@example.com", estimate.CustomerEmail);
        Assert.Contains("&amp;approve=true", estimate.EmailBody);
        Assert.Contains("&amp;approve=false", estimate.EmailBody);
        Assert.Contains("&lt;teste&gt;", estimate.EmailBody);
        Assert.DoesNotContain("<teste>", estimate.EmailBody);
        Assert.Null(estimate.EmailSentAt);
        Assert.Equal(0, (await data.Parts.List()).Single().Quantity);
        Assert.Equal(1, data.Saves);
    }

    [Fact]
    public async Task CannotEstimateBeforeDiagnosis()
    {
        var (data, _, input, useCases) = Setup();
        var received = new ServiceOrder();
        await data.Orders.Add(received);
        await Assert.ThrowsAsync<BusinessException>(() => useCases.Create(received.Id, input));
        Assert.Empty(await data.Estimates.List());
    }

    [Fact]
    public async Task InvalidEmailDoesNotMoveOrderOrQueueEmail()
    {
        var (data, order, input, useCases) = Setup();
        (await data.Customers.List()).Single().Email = null;
        await Assert.ThrowsAsync<BusinessException>(() => useCases.Create(order.Id, input));
        Assert.Equal(ServiceOrderStatus.EM_DIAGNOSTICO, order.Status);
        Assert.Empty(await data.Estimates.List());
        Assert.Equal(0, data.Saves);
    }

    [Fact]
    public async Task ConfirmationLinkCanApproveOnlyOnce()
    {
        var (data, order, input, useCases) = Setup();
        var estimate = await useCases.Create(order.Id, input);
        var token = System.Text.RegularExpressions.Regex.Match(estimate.EmailBody, "token=([A-F0-9]+)").Groups[1].Value;
        await useCases.GetForDecision(estimate.Id, token);
        Assert.Equal(ServiceOrderStatus.AGUARDANDO_APROVACAO, order.Status);
        await useCases.Decide(estimate.Id, token, true);
        Assert.Equal(ServiceOrderStatus.APROVADA, order.Status);
        await Assert.ThrowsAsync<BusinessException>(() => useCases.Decide(estimate.Id, token, false));
    }

    [Fact]
    public async Task GenericStatusCannotBypassEstimateOrCustomerDecision()
    {
        var (data, order, _, _) = Setup();
        var useCases = new ServiceOrderUseCases(data);
        await Assert.ThrowsAsync<BusinessException>(() => useCases.ChangeStatus(order.Id, ServiceOrderStatus.AGUARDANDO_APROVACAO));
        order.Transition(ServiceOrderStatus.AGUARDANDO_APROVACAO);
        await Assert.ThrowsAsync<BusinessException>(() => useCases.ChangeStatus(order.Id, ServiceOrderStatus.APROVADA));
        await Assert.ThrowsAsync<BusinessException>(() => useCases.ChangeStatus(order.Id, ServiceOrderStatus.CANCELADA));
    }

    internal class MemoryRepository<T> : IRepository<T> where T : class
    {
        private readonly List<T> items = new();
        public Task<T?> Get(Guid id) => Task.FromResult(items.FirstOrDefault(x => (Guid)typeof(T).GetProperty("Id")!.GetValue(x)! == id));
        public Task<List<T>> List() => Task.FromResult(items.ToList());
        public Task Add(T entity) { items.Add(entity); return Task.CompletedTask; }
        public void Remove(T entity) => items.Remove(entity);
    }

    internal class MemoryData : IWorkshopData
    {
        public IRepository<Estimate> Estimates { get; } = new MemoryRepository<Estimate>();
        public IRepository<Customer> Customers { get; } = new MemoryRepository<Customer>();
        public IRepository<Vehicle> Vehicles { get; } = new MemoryRepository<Vehicle>();
        public IRepository<User> Users { get; } = new MemoryRepository<User>();
        public IRepository<WorkshopService> Services { get; } = new MemoryRepository<WorkshopService>();
        public IRepository<Part> Parts { get; } = new MemoryRepository<Part>();
        public IRepository<ServiceOrder> Orders { get; } = new MemoryRepository<ServiceOrder>();
        public IRepository<StockMovement> Movements { get; } = new MemoryRepository<StockMovement>();
        public int Saves { get; private set; }
        public Task Save() { Saves++; return Task.CompletedTask; }
        public Task<ServiceOrder?> GetOrderWithItems(Guid id) => Orders.Get(id);
        public Task<List<ServiceOrder>> ListOrders() => Orders.List();
        public async Task<User?> FindUserByEmail(string email) => (await Users.List()).FirstOrDefault(x => x.Email == email);
        public async Task<Customer?> FindCustomerByDocument(string doc) => (await Customers.List()).FirstOrDefault(x => x.Document == doc);
        public async Task<Vehicle?> FindVehicleByPlate(string plate) => (await Vehicles.List()).FirstOrDefault(x => x.Plate == plate);
        public async Task<Part?> FindPartBySku(string sku) => (await Parts.List()).FirstOrDefault(x => x.Sku == sku);
        public Task<List<ServiceOrder>> CustomerOrders(string document) => throw new NotSupportedException();
    }
}
