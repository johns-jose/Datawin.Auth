using System.Data;
using DataWin.Auth.Core.Interfaces;
using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DataWin.Auth.Infrastructure.Database;

public sealed class PostgresConnectionFactory : IDbConnectionFactory
{
    private readonly string _globalConnectionString;
    private readonly IRegionalRouter _router;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PostgresConnectionFactory> _logger;

    public PostgresConnectionFactory(
        string globalConnectionString,
        IRegionalRouter router,
        IMemoryCache cache,
        ILogger<PostgresConnectionFactory> logger)
    {
        _globalConnectionString = globalConnectionString;
        _router = router;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IDbConnection> CreateGlobalConnectionAsync(CancellationToken ct = default)
    {
        var connection = new NpgsqlConnection(_globalConnectionString);
        await connection.OpenAsync(ct);
        return connection;
    }

    public async Task<IDbConnection> CreateRegionalConnectionAsync(string regionCode, CancellationToken ct = default)
    {
        var cacheKey = $"regional_conn_{regionCode}";
        if (!_cache.TryGetValue(cacheKey, out string? connectionString))
        {
            connectionString = await ResolveRegionalConnectionStringAsync(regionCode, ct);
            _cache.Set(cacheKey, connectionString, TimeSpan.FromMinutes(10));
        }

        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }

    private async Task<string> ResolveRegionalConnectionStringAsync(string regionCode, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(_globalConnectionString);
        await connection.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand("SELECT connection_string FROM regional_endpoints WHERE region_code = @region AND is_active = true", connection);
        cmd.Parameters.AddWithValue("region", regionCode);

        var result = await cmd.ExecuteScalarAsync(ct);
        return result?.ToString() ?? throw new InvalidOperationException($"No active regional endpoint for region: {regionCode}");
    }
}
