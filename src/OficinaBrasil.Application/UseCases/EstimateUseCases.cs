using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using OficinaBrasil.Domain;

namespace OficinaBrasil.Application;

public class EstimateSettings
{
    public string PublicBaseUrl { get; set; } = "";
    public int ValidityHours { get; set; } = 72;
}

public class EstimateUseCases(IWorkshopData data, EstimateSettings settings)
{
    public async Task<Estimate> Create(Guid orderId, EstimateInput input)
    {
        // [Etapa 1: validar o diagnóstico, os itens, o e-mail do cliente e a configuração dos links]
        var order = await data.GetOrderWithItems(orderId) ?? throw new KeyNotFoundException("OS não encontrada");
        if (order.Status != ServiceOrderStatus.EM_DIAGNOSTICO)
            throw new BusinessException("O orçamento só pode ser cadastrado durante o diagnóstico");
        Checks.Required(input.Diagnosis, "Diagnóstico");
        if (input.Services == null || input.Services.Count == 0)
            throw new BusinessException("Informe ao menos um serviço no orçamento");
        if (input.Services.Select(x => x.ServiceId).Distinct().Count() != input.Services.Count)
            throw new BusinessException("Serviço duplicado");
        var parts = input.Parts ?? new();
        if (parts.Select(x => x.PartId).Distinct().Count() != parts.Count)
            throw new BusinessException("Peça duplicada");
        var vehicle = await data.Vehicles.Get(order.VehicleId) ?? throw new KeyNotFoundException("Veículo não encontrado");
        var customer = await data.Customers.Get(vehicle.CustomerId) ?? throw new KeyNotFoundException("Cliente não encontrado");
        if (string.IsNullOrWhiteSpace(customer.Email) || !MailAddress.TryCreate(customer.Email, out _))
            throw new BusinessException("Cadastre um e-mail válido para o cliente antes de gerar o orçamento");
        if (!Uri.TryCreate(settings.PublicBaseUrl, UriKind.Absolute, out var baseUrl) ||
            (baseUrl.Scheme != "https" && !(baseUrl.Scheme == "http" && baseUrl.IsLoopback)) ||
            settings.ValidityHours is < 1 or > 720)
            throw new BusinessException("Configure Estimates:PublicBaseUrl (HTTPS) e ValidityHours (1 a 720)");

        // [Etapa 2: consultar o catálogo e capturar os preços dos serviços e das peças]
        var serviceItems = new List<OrderServiceItem>();
        var partItems = new List<OrderPartItem>();
        var lines = new List<string>();
        var culture = CultureInfo.GetCultureInfo("pt-BR");
        foreach (var item in input.Services)
        {
            var service = await data.Services.Get(item.ServiceId) ?? throw new KeyNotFoundException("Serviço não encontrado");
            if (!service.Active) throw new BusinessException("Serviço inativo");
            serviceItems.Add(new OrderServiceItem { ServiceOrderId = order.Id, WorkshopServiceId = service.Id, UnitPrice = service.Price });
            lines.Add($"Serviço: {service.Name} — {service.Price.ToString("C", culture)}");
        }
        foreach (var item in parts)
        {
            if (item.Quantity <= 0) throw new BusinessException("Quantidade inválida");
            var part = await data.Parts.Get(item.PartId) ?? throw new KeyNotFoundException("Peça não encontrada");
            partItems.Add(new OrderPartItem { ServiceOrderId = order.Id, PartId = part.Id, Quantity = item.Quantity, UnitPrice = part.UnitPrice });
            lines.Add($"Peça: {part.Name} — {item.Quantity} x {part.UnitPrice.ToString("C", culture)}");
        }
        // [Etapa 3: substituir os itens preliminares somente após validar todo o orçamento]
        order.Services.Clear();
        order.Services.AddRange(serviceItems);
        order.Parts.Clear();
        order.Parts.AddRange(partItems);
        // [Etapa 4: gerar o token de decisão e preparar o orçamento e o e-mail pendente]
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var estimate = new Estimate
        {
            ServiceOrderId = order.Id, Diagnosis = input.Diagnosis.Trim(), Summary = string.Join("\n", lines),
            Total = order.Total, CustomerEmail = customer.Email,
            TokenHash = Estimate.HashToken(token), ExpiresAt = DateTime.UtcNow.AddHours(settings.ValidityHours)
        };
        estimate.EmailBody = BuildEmail(estimate, customer.Name, token);
        // [Etapa 5: colocar a OS em espera e salvar orçamento, itens e e-mail na mesma transação]
        order.Transition(ServiceOrderStatus.AGUARDANDO_APROVACAO);
        await data.Estimates.Add(estimate);
        await data.Save();
        return estimate;
    }

    public async Task<Estimate> Reissue(Guid orderId)
    {
        // [Etapa 1: localizar o orçamento pendente e validar a configuração e o destinatário]
        var order = await data.GetOrderWithItems(orderId) ?? throw new KeyNotFoundException("OS não encontrada");
        var estimate = order.Estimate ?? throw new BusinessException("A OS não possui orçamento");
        if (order.Status != ServiceOrderStatus.AGUARDANDO_APROVACAO)
            throw new BusinessException("A OS não está aguardando aprovação");
        if (!Uri.TryCreate(settings.PublicBaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback)) || settings.ValidityHours is < 1 or > 720)
            throw new BusinessException("Configure Estimates:PublicBaseUrl e ValidityHours");
        var vehicle = await data.Vehicles.Get(order.VehicleId) ?? throw new KeyNotFoundException("Veículo não encontrado");
        var customer = await data.Customers.Get(vehicle.CustomerId) ?? throw new KeyNotFoundException("Cliente não encontrado");
        if (string.IsNullOrWhiteSpace(customer.Email) || !MailAddress.TryCreate(customer.Email, out _))
            throw new BusinessException("Cadastre um e-mail válido para o cliente");
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        // [Etapa 2: renovar a validade e invalidar o token anterior sem alterar os valores]
        estimate.Reissue(Estimate.HashToken(token), DateTime.UtcNow.AddHours(settings.ValidityHours));
        // [Etapa 3: preparar o novo e-mail e salvar a reemissão para envio pelo worker]
        estimate.CustomerEmail = customer.Email;
        estimate.EmailBody = BuildEmail(estimate, customer.Name, token);
        await data.Save();
        return estimate;
    }

    private string BuildEmail(Estimate estimate, string name, string token)
    {
        var culture = CultureInfo.GetCultureInfo("pt-BR");
        var url = settings.PublicBaseUrl.TrimEnd('/') + $"/api/estimates/{estimate.Id}/decision?token={token}";
        return $"<h1>Orçamento da OS {estimate.ServiceOrderId}</h1><p>Olá, {WebUtility.HtmlEncode(name)}.</p>" +
            $"<p>Diagnóstico: {WebUtility.HtmlEncode(estimate.Diagnosis)}</p><pre>{WebUtility.HtmlEncode(estimate.Summary)}</pre>" +
            $"<p>Total: {estimate.Total.ToString("C", culture)}</p><p>Link válido até {estimate.ExpiresAt:yyyy-MM-dd HH:mm} UTC.</p>" +
            $"<p><a href=\"{WebUtility.HtmlEncode(url + "&approve=true")}\">Aprovar orçamento</a></p>" +
            $"<p><a href=\"{WebUtility.HtmlEncode(url + "&approve=false")}\">Reprovar orçamento</a></p>";
    }

    public async Task<Estimate> GetForDecision(Guid id, string token)
    {
        var estimate = await data.Estimates.Get(id) ?? throw new KeyNotFoundException("Orçamento não encontrado");
        estimate.ValidateToken(token, DateTime.UtcNow);
        return estimate;
    }

    public async Task Decide(Guid id, string token, bool approve)
    {
        // [Etapa 1: validar o token e localizar a OS vinculada ao orçamento]
        var estimate = await GetForDecision(id, token);
        var order = await data.GetOrderWithItems(estimate.ServiceOrderId) ?? throw new KeyNotFoundException("OS não encontrada");
        // [Etapa 2: registrar a decisão e mover a OS para APROVADA ou CANCELADA]
        estimate.Decide(order, token, approve, DateTime.UtcNow);
        // [Etapa 3: persistir a decisão e o estado da OS na mesma transação]
        await data.Save();
    }
}
