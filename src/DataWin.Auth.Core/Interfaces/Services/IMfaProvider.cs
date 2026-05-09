using DataWin.Auth.Core.Enums;
using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Interfaces.Services;

public interface IMfaProvider
{
    MfaMethod Method { get; }
    Task<string> EnrollAsync(UuidV7 tenantId, UuidV7 userId, CancellationToken ct = default);
    Task<bool> VerifyAsync(UuidV7 tenantId, UuidV7 userId, string code, CancellationToken ct = default);
    Task DisableAsync(UuidV7 tenantId, UuidV7 userId, CancellationToken ct = default);
}
