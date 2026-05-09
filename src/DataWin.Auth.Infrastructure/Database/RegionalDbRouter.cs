using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;

namespace DataWin.Auth.Infrastructure.Database;

public sealed class RegionalDbRouter : IRegionalRouter
{
    private readonly string _globalConnectionString;
    private readonly IMemoryCache _cache;

    public RegionalDbRouter(string globalConnectionString, IMemoryCache cache)
    {
        _globalConnectionString = globalConnectionString;
        _cache = cache;
    }

    public async Task<string> ResolveConnectionStringAsync(UuidV7 tenantId, CancellationToken ct = default)
    {
        var region = await ResolvePrimaryRegionAsync(tenantId, ct);

        var cacheKey = $"conn_{region.Value}";
        if (_cache.TryGetValue(cacheKey, out string? cached))
            return cached!;

        await using var conn = new NpgsqlConnection(_globalConnectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(
            "SELECT connection_string FROM regional_endpoints WHERE region_code = @region AND is_active = true", conn);
        cmd.Parameters.AddWithValue("region", region.Value);

        var result = await cmd.ExecuteScalarAsync(ct);
        var connStr = result?.ToString()
            ?? throw new InvalidOperationException($"No active endpoint for region: {region.Value}");

        _cache.Set(cacheKey, connStr, TimeSpan.FromMinutes(10));
        return connStr;
    }

    public async Task<RegionCode> ResolvePrimaryRegionAsync(UuidV7 tenantId, CancellationToken ct = default)
    {
        var cacheKey = $"tenant_region_{tenantId}";
        if (_cache.TryGetValue(cacheKey, out RegionCode cached))
            return cached;

        await using var conn = new NpgsqlConnection(_globalConnectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(
            "SELECT region_code FROM tenant_regions WHERE tenant_id = @tid AND is_primary = true", conn);
        cmd.Parameters.AddWithValue("tid", (Guid)tenantId);

        var result = await cmd.ExecuteScalarAsync(ct);
        var region = new RegionCode(result?.ToString()
            ?? throw new InvalidOperationException($"No primary region for tenant: {tenantId}"));

        _cache.Set(cacheKey, region, TimeSpan.FromMinutes(5));
        return region;
    }
}
