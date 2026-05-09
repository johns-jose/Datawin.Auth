using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Interfaces.Services;

public interface ITokenService
{
    string GenerateAccessToken(UuidV7 userId, UuidV7 tenantId, string region, IReadOnlyList<string> roles, bool mfaVerified, string audience);
    string GenerateRefreshToken();
    Task<(bool IsValid, UuidV7 UserId, UuidV7 TenantId)> ValidateAccessTokenAsync(string token, CancellationToken ct = default);
}
