using OficinaBrasil.Application;
using OficinaBrasil.Domain;
using Xunit;
using Data = OficinaBrasil.Domain.Tests.EstimateUseCasesTests.MemoryData;

namespace OficinaBrasil.Domain.Tests;

public class BusinessRulesTests
{
    [Theory]
    [InlineData("11.222.333/0001-81", "11222333000181")]
    [InlineData("529.982.247-25", "52998224725")]
    public void DocumentsNormalizeAndValueObjectsCompareByValue(string value, string expected)
    {
        var document = new DocumentNumber(value);
        Assert.Equal(expected, document.Value);
        Assert.Equal(expected, document.ToString());
        Assert.Equal(document, new DocumentNumber(expected));
    }

    [Theory]
    [InlineData("00000000000000")]
    [InlineData("11111111111111")]
    [InlineData("11222333000180")]
    [InlineData("11222333000182")]
    [InlineData("52998224724")]
    [InlineData("123")]
    [InlineData("")]
    [InlineData("ABC11222333000181")]
    public void InvalidDocumentsAreRejected(string input) => Assert.Throws<BusinessException>(() => new DocumentNumber(input));

    [Fact]
    public void PlateValueObjectNormalizes()
    {
        Assert.Equal(new LicensePlate("ABC1234"), new LicensePlate("abc-1234"));
        Assert.Equal("ABC1234", new LicensePlate("abc-1234").ToString());
        Assert.Throws<BusinessException>(() => new LicensePlate("ABC@1234"));
    }

    [Theory]
    [InlineData(ServiceOrderStatus.RECEBIDA)]
    [InlineData(ServiceOrderStatus.EM_DIAGNOSTICO)]
    [InlineData(ServiceOrderStatus.AGUARDANDO_APROVACAO)]
    [InlineData(ServiceOrderStatus.APROVADA)]
    public void CancellationIsAllowedBeforeExecution(ServiceOrderStatus status)
    {
        var order = new ServiceOrder();
        foreach (var step in new[] { ServiceOrderStatus.EM_DIAGNOSTICO, ServiceOrderStatus.AGUARDANDO_APROVACAO, ServiceOrderStatus.APROVADA })
        { if (order.Status == status) break; order.Transition(step); }
        order.Transition(ServiceOrderStatus.CANCELADA);
        Assert.Equal(ServiceOrderStatus.CANCELADA, order.Status);
        Assert.Throws<BusinessException>(() => order.Transition(ServiceOrderStatus.EM_DIAGNOSTICO));
    }

    [Fact]
    public void StockCannotOverflowOrBecomeNegative()
    {
        var part = new Part { Quantity = int.MaxValue };
        Assert.Throws<BusinessException>(() => part.AdjustStock(1));
        Assert.Equal(int.MaxValue, part.Quantity);
        part.Quantity = 0;
        Assert.Throws<BusinessException>(() => part.AdjustStock(-1));
    }

    [Fact]
    public async Task CustomerCreationLookupAndDuplicateRules()
    {
        var data = new Data();
        var cases = new CustomerUseCases(data);
        var customer = await cases.CreateCustomer(new CustomerInput(" Maria ", "529.982.247-25", "maria@example.test", "11955550000"));
        Assert.Equal("Maria", customer.Name);
        Assert.Same(customer, await cases.FindByDocument("529.982.247-25"));
        await Assert.ThrowsAsync<BusinessException>(() => cases.CreateCustomer(new CustomerInput("Outra", customer.Document, null, null)));
        await Assert.ThrowsAsync<BusinessException>(() => cases.CreateCustomer(new CustomerInput("", customer.Document, null, null)));
    }

    [Fact]
    public async Task VehicleValidatesOwnerPlateAndYear()
    {
        var data = new Data();
        var cases = new VehicleUseCases(data);
        var customer = new Customer();
        var input = new VehicleInput(customer.Id, "ABC-1234", "Fiat", "Argo", 2020);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => cases.CreateVehicle(input));
        await data.Customers.Add(customer);
        var vehicle = await cases.CreateVehicle(input);
        Assert.Equal("ABC1234", vehicle.Plate);
        await Assert.ThrowsAsync<BusinessException>(() => cases.CreateVehicle(input));
        input.Plate = "DEF1234";
        input.Year = 1800;
        await Assert.ThrowsAsync<BusinessException>(() => cases.CreateVehicle(input));
        input.Year = DateTime.UtcNow.Year + 2;
        await Assert.ThrowsAsync<BusinessException>(() => cases.CreateVehicle(input));
        input.Year = 2020;
        input.Brand = "";
        await Assert.ThrowsAsync<BusinessException>(() => cases.CreateVehicle(input));
    }

    [Fact]
    public async Task CatalogRejectsMissingNameAndNegativePrice()
    {
        var cases = new ServiceCatalogUseCases(new Data());
        var service = await cases.CreateService(new ServiceInput("Troca", "Óleo", 120));
        Assert.Equal(120, service.Price);
        await Assert.ThrowsAsync<BusinessException>(() => cases.CreateService(new ServiceInput("", null, 120)));
        await Assert.ThrowsAsync<BusinessException>(() => cases.CreateService(new ServiceInput("Troca", null, -1)));
    }

    [Fact]
    public async Task InventoryRecordsInitialBalanceAndEveryMovement()
    {
        var data = new Data();
        var cases = new InventoryUseCases(data);
        var part = await cases.CreatePart(new PartInput("Filtro", "F1", "Óleo", 20, 2));
        Assert.Single(await data.Movements.List());
        await cases.MoveStock(part.Id, new StockInput(-1, "Consumo"));
        Assert.Equal(1, part.Quantity);
        Assert.Equal(2, (await data.Movements.List()).Count);
        await Assert.ThrowsAsync<BusinessException>(() => cases.MoveStock(part.Id, new StockInput(-2, "Consumo")));
        await Assert.ThrowsAsync<BusinessException>(() => cases.MoveStock(part.Id, new StockInput(0, "Consumo")));
        await Assert.ThrowsAsync<BusinessException>(() => cases.MoveStock(part.Id, new StockInput(1, "")));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => cases.MoveStock(Guid.NewGuid(), new StockInput(1, "Entrada")));
        await Assert.ThrowsAsync<BusinessException>(() => cases.CreatePart(new PartInput("Filtro", "F1", null, 20, 0)));
        await Assert.ThrowsAsync<BusinessException>(() => cases.CreatePart(new PartInput("Filtro", "F2", null, -1, 0)));
        await Assert.ThrowsAsync<BusinessException>(() => cases.CreatePart(new PartInput("Filtro", "F2", null, 1, -1)));
        await cases.CreatePart(new PartInput("Sem estoque", "F3", null, 1, 0));
        Assert.Equal(2, (await data.Movements.List()).Count);
    }

    [Fact]
    public async Task OrderCreationCapturesCatalogPricesAndRejectsInvalidItems()
    {
        var (data, existing, input, _) = EstimateUseCasesTests.Setup();
        var cases = new ServiceOrderUseCases(data);
        var orderInput = new OrderInput(existing.VehicleId, "Teste", input.Services, input.Parts);
        var order = await cases.CreateOrder(orderInput);
        Assert.Equal(200, order.Total);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => cases.CreateOrder(new OrderInput(Guid.NewGuid(), null, new(), new())));
        orderInput.Services.Add(orderInput.Services[0]);
        await Assert.ThrowsAsync<BusinessException>(() => cases.CreateOrder(orderInput));
        orderInput.Services.RemoveAt(1);
        orderInput.Parts.Add(orderInput.Parts[0]);
        await Assert.ThrowsAsync<BusinessException>(() => cases.CreateOrder(orderInput));
        orderInput.Parts.RemoveAt(1);
        orderInput.Parts[0].Quantity = 0;
        await Assert.ThrowsAsync<BusinessException>(() => cases.CreateOrder(orderInput));
        orderInput.Parts[0].Quantity = 1;
        var service = (await data.Services.List()).Single();
        service.Active = false;
        await Assert.ThrowsAsync<BusinessException>(() => cases.CreateOrder(orderInput));
        service.Active = true;
        orderInput.Services = new() { new(Guid.NewGuid()) };
        await Assert.ThrowsAsync<KeyNotFoundException>(() => cases.CreateOrder(orderInput));
        orderInput.Services = new();
        orderInput.Parts = new() { new(Guid.NewGuid(), 1) };
        await Assert.ThrowsAsync<KeyNotFoundException>(() => cases.CreateOrder(orderInput));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => cases.ChangeStatus(Guid.NewGuid(), ServiceOrderStatus.EM_DIAGNOSTICO));
    }

    [Fact]
    public async Task ExecutionCannotConsumeMissingOrInsufficientParts()
    {
        var (data, order, _, _) = EstimateUseCasesTests.Setup();
        var part = (await data.Parts.List()).Single();
        order.Parts.Add(new OrderPartItem { PartId = part.Id, Quantity = 1 });
        order.Transition(ServiceOrderStatus.AGUARDANDO_APROVACAO);
        order.Transition(ServiceOrderStatus.APROVADA);
        var cases = new ServiceOrderUseCases(data);
        await Assert.ThrowsAsync<BusinessException>(() => cases.ChangeStatus(order.Id, ServiceOrderStatus.EM_EXECUCAO));
        Assert.Equal(ServiceOrderStatus.APROVADA, order.Status);
        data.Parts.Remove(part);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => cases.ChangeStatus(order.Id, ServiceOrderStatus.EM_EXECUCAO));
    }

    [Fact]
    public async Task EstimatesValidateItemsAndConfigurationBeforeMutatingOrder()
    {
        foreach (var failure in new[] { "diagnosis", "empty-services", "duplicate-service", "duplicate-part", "quantity", "service-missing", "part-missing", "inactive", "settings" })
        {
            var (data, order, input, cases) = EstimateUseCasesTests.Setup();
            switch (failure)
            {
                case "diagnosis": input.Diagnosis = ""; break;
                case "empty-services": input.Services.Clear(); break;
                case "duplicate-service": input.Services.Add(input.Services[0]); break;
                case "duplicate-part": input.Parts.Add(input.Parts[0]); break;
                case "quantity": input.Parts[0].Quantity = -1; break;
                case "service-missing": input.Services[0].ServiceId = Guid.NewGuid(); break;
                case "part-missing": input.Parts[0].PartId = Guid.NewGuid(); break;
                case "inactive": (await data.Services.List()).Single().Active = false; break;
                case "settings": cases = new EstimateUseCases(data, new EstimateSettings()); break;
            }
            if (failure.EndsWith("missing")) await Assert.ThrowsAsync<KeyNotFoundException>(() => cases.Create(order.Id, input));
            else await Assert.ThrowsAsync<BusinessException>(() => cases.Create(order.Id, input));
            Assert.Equal(ServiceOrderStatus.EM_DIAGNOSTICO, order.Status);
            Assert.Equal(0, data.Saves);
        }
    }

    [Fact]
    public async Task MissingOrdersAndEstimatesReturnNotFound()
    {
        var (_, _, input, cases) = EstimateUseCasesTests.Setup();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => cases.Create(Guid.NewGuid(), input));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => cases.GetForDecision(Guid.NewGuid(), "token"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => cases.Reissue(Guid.NewGuid()));
    }

    [Fact]
    public void ReissueRejectsAnsweredBudgetAndInvalidatesPreviousToken()
    {
        var order = new ServiceOrder();
        order.Transition(ServiceOrderStatus.EM_DIAGNOSTICO);
        order.Transition(ServiceOrderStatus.AGUARDANDO_APROVACAO);
        var estimate = new Estimate { ServiceOrderId = order.Id, TokenHash = Estimate.HashToken("old"), ExpiresAt = DateTime.UtcNow.AddHours(1) };
        estimate.Reissue(Estimate.HashToken("new"), DateTime.UtcNow.AddDays(1));
        Assert.Throws<BusinessException>(() => estimate.ValidateToken("old", DateTime.UtcNow));
        estimate.Decide(order, "new", false, DateTime.UtcNow);
        Assert.Throws<BusinessException>(() => estimate.Reissue(Estimate.HashToken("other"), DateTime.UtcNow.AddDays(1)));
    }
}
