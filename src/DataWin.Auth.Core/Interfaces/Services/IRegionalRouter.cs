using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Interfaces.Services;

public interface IRegionalRouter
{
    Task<string> ResolveConnectionStringAsync(UuidV7 tenantId, CancellationToken ct = default);
    Task<RegionCode> ResolvePrimaryRegionAsync(UuidV7 tenantId, CancellationToken ct = default);
}
