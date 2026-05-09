using DataWin.Auth.Core.Entities;
using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Interfaces.Repositories;

public interface IOAuthClientRepository
{
    Task<OAuthClient?> GetByClientIdAsync(UuidV7 tenantId, string clientId, CancellationToken ct = default);
    Task CreateAsync(OAuthClient client, CancellationToken ct = default);
    Task UpdateAsync(OAuthClient client, CancellationToken ct = default);
    Task<SamlConfiguration?> GetSamlConfigAsync(UuidV7 tenantId, string entityId, CancellationToken ct = default);
    Task CreateSamlConfigAsync(SamlConfiguration config, CancellationToken ct = default);
    Task UpdateSamlConfigAsync(SamlConfiguration config, CancellationToken ct = default);
}
