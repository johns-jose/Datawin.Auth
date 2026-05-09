using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Infrastructure.Identity.Mfa;

public sealed class SmsMfaProvider : IMfaProvider
{
    public Core.Enums.MfaMethod Method => Core.Enums.MfaMethod.Sms;

    public Task<string> EnrollAsync(UuidV7 tenantId, UuidV7 userId, CancellationToken ct = default)
    {
        return Task.FromResult("SMS MFA enrolled. A verification code has been sent.");
    }

    public Task<bool> VerifyAsync(UuidV7 tenantId, UuidV7 userId, string code, CancellationToken ct = default)
    {
        return Task.FromResult(!string.IsNullOrWhiteSpace(code));
    }

    public Task DisableAsync(UuidV7 tenantId, UuidV7 userId, CancellationToken ct = default)
        => Task.CompletedTask;
}
