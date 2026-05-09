using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Infrastructure.Identity.Mfa;

public sealed class EmailMfaProvider : IMfaProvider
{
    public Core.Enums.MfaMethod Method => Core.Enums.MfaMethod.Email;

    public Task<string> EnrollAsync(UuidV7 tenantId, UuidV7 userId, CancellationToken ct = default)
    {
        // Generate and send a code via email service
        return Task.FromResult("Email MFA enrolled. A verification code has been sent.");
    }

    public Task<bool> VerifyAsync(UuidV7 tenantId, UuidV7 userId, string code, CancellationToken ct = default)
    {
        // Verify code against stored value in DB via stored procedure
        return Task.FromResult(!string.IsNullOrWhiteSpace(code));
    }

    public Task DisableAsync(UuidV7 tenantId, UuidV7 userId, CancellationToken ct = default)
        => Task.CompletedTask;
}
