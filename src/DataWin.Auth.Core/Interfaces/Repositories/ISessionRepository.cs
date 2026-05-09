using DataWin.Auth.Core.Entities;
using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Interfaces.Repositories;

public interface ISessionRepository
{
    Task<Session?> GetByIdAsync(UuidV7 tenantId, UuidV7 sessionId, CancellationToken ct = default);
    Task CreateAsync(Session session, CancellationToken ct = default);
    Task RevokeAsync(UuidV7 tenantId, UuidV7 sessionId, CancellationToken ct = default);
    Task RevokeAllForUserAsync(UuidV7 tenantId, UuidV7 userId, CancellationToken ct = default);
    Task<RefreshToken?> GetRefreshTokenByHashAsync(UuidV7 tenantId, string tokenHash, CancellationToken ct = default);
    Task CreateRefreshTokenAsync(RefreshToken token, CancellationToken ct = default);
    Task RevokeRefreshTokenAsync(UuidV7 tenantId, UuidV7 tokenId, UuidV7? replacedBy, CancellationToken ct = default);
}
