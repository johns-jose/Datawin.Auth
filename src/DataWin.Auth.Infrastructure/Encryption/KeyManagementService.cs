using System.Security.Cryptography;
using DataWin.Auth.Core.Interfaces;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DataWin.Auth.Infrastructure.Encryption;

public sealed class KeyManagementService : IKeyManagementService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly Core.Interfaces.Services.IRegionalRouter _router;
    private readonly IMemoryCache _cache;
    private readonly ILogger<KeyManagementService> _logger;

    public KeyManagementService(
        IDbConnectionFactory connectionFactory,
        Core.Interfaces.Services.IRegionalRouter router,
        IMemoryCache cache,
        ILogger<KeyManagementService> logger)
    {
        _connectionFactory = connectionFactory;
        _router = router;
        _cache = cache;
        _logger = logger;
    }

    public (byte[] Key, string KeyId) GetCurrentKey(UuidV7 tenantId)
    {
        var cacheKey = $"enc_key_{tenantId}";
        if (_cache.TryGetValue(cacheKey, out (byte[] Key, string KeyId) cached))
            return cached;

        // In production, this would fetch from a secure key vault or HSM
        // For now, derive a tenant-scoped key deterministically (replace with real KMS)
        var keyId = $"dek_{tenantId}";
        var key = DeriveKey(tenantId, keyId);
        _cache.Set(cacheKey, (key, keyId), TimeSpan.FromMinutes(30));
        return (key, keyId);
    }

    public byte[] GetKeyById(UuidV7 tenantId, string keyId)
    {
        // In production, retrieve specific key version from vault
        return DeriveKey(tenantId, keyId);
    }

    public async Task DestroyKeyAsync(UuidV7 tenantId, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(tenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand("CALL sp_encryption_key_destroy(@p_tenant_id)", conn);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)tenantId);
        await cmd.ExecuteNonQueryAsync(ct);

        _cache.Remove($"enc_key_{tenantId}");
        _logger.LogWarning("Encryption key destroyed for tenant {TenantId}", tenantId);
    }

    private static byte[] DeriveKey(UuidV7 tenantId, string keyId)
    {
        var ikm = System.Text.Encoding.UTF8.GetBytes($"{tenantId}:{keyId}");
        return HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, 32, info: System.Text.Encoding.UTF8.GetBytes("datawin-auth-dek"));
    }
}
