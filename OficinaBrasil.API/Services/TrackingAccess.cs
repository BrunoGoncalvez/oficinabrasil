using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace OficinaBrasil.API.Services;

public class TrackingAccess(IDataProtectionProvider provider)
{
    private readonly ITimeLimitedDataProtector protector = provider.CreateProtector("OficinaBrasil.Tracking.v1").ToTimeLimitedDataProtector();
    public string Issue(Guid orderId) => protector.Protect(orderId.ToString(), TimeSpan.FromDays(30));
    public bool Allows(Guid orderId, string token)
    {
        try { return Guid.TryParse(protector.Unprotect(token), out var allowedId) && allowedId == orderId; }
        catch (CryptographicException) { return false; }
        catch (ArgumentException) { return false; }
    }
}
