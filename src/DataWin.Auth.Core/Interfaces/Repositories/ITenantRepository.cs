using DataWin.Auth.Core.Entities;
using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Interfaces.Repositories;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(UuidV7 id, CancellationToken ct = default);
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Tenant?> GetByDomainAsync(string domain, CancellationToken ct = default);
    Task CreateAsync(Tenant tenant, CancellationToken ct = default);
    Task UpdateAsync(Tenant tenant, CancellationToken ct = default);
    Task<IReadOnlyList<TenantRegion>> GetRegionsAsync(UuidV7 tenantId, CancellationToken ct = default);
    Task AddRegionAsync(TenantRegion region, CancellationToken ct = default);
    Task RemoveRegionAsync(UuidV7 tenantId, RegionCode regionCode, CancellationToken ct = default);
}
