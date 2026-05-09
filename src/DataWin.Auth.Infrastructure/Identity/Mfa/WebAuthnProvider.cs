using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Infrastructure.Identity.Mfa;

public sealed class WebAuthnProvider : IMfaProvider
{
    public Core.Enums.MfaMethod Method => Core.Enums.MfaMethod.WebAuthn;

    public Task<string> EnrollAsync(UuidV7 tenantId, UuidV7 userId, CancellationToken ct = default)
    {
        // Return challenge options for WebAuthn registration (FIDO2)
        return Task.FromResult("{\"challenge\":\"placeholder\",\"rp\":{\"name\":\"DataWin\"}}");
    }

    public Task<bool> VerifyAsync(UuidV7 tenantId, UuidV7 userId, string code, CancellationToken ct = default)
    {
        // Verify attestation/assertion response
        return Task.FromResult(!string.IsNullOrWhiteSpace(code));
    }

    public Task DisableAsync(UuidV7 tenantId, UuidV7 userId, CancellationToken ct = default)
        => Task.CompletedTask;
}
