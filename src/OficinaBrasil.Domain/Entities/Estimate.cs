using System.Security.Cryptography;
using System.Text;

namespace OficinaBrasil.Domain;

public class Estimate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServiceOrderId { get; set; }
    public string Diagnosis { get; set; } = "";
    public string Summary { get; set; } = "";
    public decimal Total { get; set; }
    public string CustomerEmail { get; set; } = "";
    public string TokenHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool? Approved { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public Guid Version { get; private set; } = Guid.NewGuid();
    public string EmailBody { get; set; } = "";
    public DateTime? EmailSentAt { get; set; }

    public static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public void ValidateToken(string token, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(token) || !CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(HashToken(token)), Encoding.UTF8.GetBytes(TokenHash)))
            throw new BusinessException("Link de orçamento inválido");
        if (now >= ExpiresAt) throw new BusinessException("Link de orçamento expirado");
    }

    public void Decide(ServiceOrder order, string token, bool approve, DateTime now)
    {
        ValidateToken(token, now);
        if (Approved.HasValue) throw new BusinessException("Este orçamento já foi respondido");
        if (order.Id != ServiceOrderId || order.Status != ServiceOrderStatus.AGUARDANDO_APROVACAO)
            throw new BusinessException("A OS não está aguardando aprovação deste orçamento");
        order.Transition(approve ? ServiceOrderStatus.APROVADA : ServiceOrderStatus.CANCELADA);
        Approved = approve;
        DecidedAt = now;
        Version = Guid.NewGuid();
    }

    public void Reissue(string tokenHash, DateTime expiresAt)
    {
        if (Approved.HasValue) throw new BusinessException("Orçamento respondido não pode ser reemitido");
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        EmailSentAt = null;
        Version = Guid.NewGuid();
    }
}
