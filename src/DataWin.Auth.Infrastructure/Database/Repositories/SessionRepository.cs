using DataWin.Auth.Core.Entities;
using DataWin.Auth.Core.Interfaces;
using DataWin.Auth.Core.Interfaces.Repositories;
using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;
using Npgsql;

namespace DataWin.Auth.Infrastructure.Database.Repositories;

public sealed class SessionRepository : ISessionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IRegionalRouter _router;

    public SessionRepository(IDbConnectionFactory connectionFactory, IRegionalRouter router)
    {
        _connectionFactory = connectionFactory;
        _router = router;
    }

    public async Task<Session?> GetByIdAsync(UuidV7 tenantId, UuidV7 sessionId, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(tenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand("SELECT * FROM sp_session_get_by_id(@p_tenant_id, @p_session_id)", conn);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)tenantId);
        cmd.Parameters.AddWithValue("p_session_id", (Guid)sessionId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return new Session
        {
            Id = UuidV7.From(reader.GetGuid(reader.GetOrdinal("id"))),
            UserId = UuidV7.From(reader.GetGuid(reader.GetOrdinal("user_id"))),
            TenantId = UuidV7.From(reader.GetGuid(reader.GetOrdinal("tenant_id"))),
            DeviceFingerprint = reader.GetString(reader.GetOrdinal("device_fingerprint")),
            IpAddress = reader.GetString(reader.GetOrdinal("ip_address")),
            UserAgent = reader.IsDBNull(reader.GetOrdinal("user_agent")) ? null : reader.GetString(reader.GetOrdinal("user_agent")),
            IsRevoked = reader.GetBoolean(reader.GetOrdinal("is_revoked")),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
            ExpiresAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("expires_at"))
        };
    }

    public async Task CreateAsync(Session session, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(session.TenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand(
            "CALL sp_session_create(@p_id, @p_user_id, @p_tenant_id, @p_device_fingerprint, @p_ip_address, @p_user_agent, @p_created_at, @p_expires_at)", conn);
        cmd.Parameters.AddWithValue("p_id", (Guid)session.Id);
        cmd.Parameters.AddWithValue("p_user_id", (Guid)session.UserId);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)session.TenantId);
        cmd.Parameters.AddWithValue("p_device_fingerprint", session.DeviceFingerprint);
        cmd.Parameters.AddWithValue("p_ip_address", session.IpAddress);
        cmd.Parameters.AddWithValue("p_user_agent", (object?)session.UserAgent ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_created_at", session.CreatedAt);
        cmd.Parameters.AddWithValue("p_expires_at", session.ExpiresAt);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task RevokeAsync(UuidV7 tenantId, UuidV7 sessionId, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(tenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand("CALL sp_session_revoke(@p_tenant_id, @p_session_id)", conn);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)tenantId);
        cmd.Parameters.AddWithValue("p_session_id", (Guid)sessionId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task RevokeAllForUserAsync(UuidV7 tenantId, UuidV7 userId, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(tenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand("CALL sp_session_revoke_all_for_user(@p_tenant_id, @p_user_id)", conn);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)tenantId);
        cmd.Parameters.AddWithValue("p_user_id", (Guid)userId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<RefreshToken?> GetRefreshTokenByHashAsync(UuidV7 tenantId, string tokenHash, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(tenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand("SELECT * FROM sp_token_get_by_hash(@p_tenant_id, @p_token_hash)", conn);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)tenantId);
        cmd.Parameters.AddWithValue("p_token_hash", tokenHash);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return new RefreshToken
        {
            Id = UuidV7.From(reader.GetGuid(reader.GetOrdinal("id"))),
            SessionId = UuidV7.From(reader.GetGuid(reader.GetOrdinal("session_id"))),
            UserId = UuidV7.From(reader.GetGuid(reader.GetOrdinal("user_id"))),
            TenantId = UuidV7.From(reader.GetGuid(reader.GetOrdinal("tenant_id"))),
            TokenHash = reader.GetString(reader.GetOrdinal("token_hash")),
            IsRevoked = reader.GetBoolean(reader.GetOrdinal("is_revoked")),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
            ExpiresAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("expires_at")),
            ReplacedByTokenId = reader.IsDBNull(reader.GetOrdinal("replaced_by_token_id")) ? null : UuidV7.From(reader.GetGuid(reader.GetOrdinal("replaced_by_token_id")))
        };
    }

    public async Task CreateRefreshTokenAsync(RefreshToken token, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(token.TenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand(
            "CALL sp_token_create_refresh(@p_id, @p_session_id, @p_user_id, @p_tenant_id, @p_token_hash, @p_created_at, @p_expires_at)", conn);
        cmd.Parameters.AddWithValue("p_id", (Guid)token.Id);
        cmd.Parameters.AddWithValue("p_session_id", (Guid)token.SessionId);
        cmd.Parameters.AddWithValue("p_user_id", (Guid)token.UserId);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)token.TenantId);
        cmd.Parameters.AddWithValue("p_token_hash", token.TokenHash);
        cmd.Parameters.AddWithValue("p_created_at", token.CreatedAt);
        cmd.Parameters.AddWithValue("p_expires_at", token.ExpiresAt);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task RevokeRefreshTokenAsync(UuidV7 tenantId, UuidV7 tokenId, UuidV7? replacedBy, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(tenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand("CALL sp_token_revoke(@p_tenant_id, @p_token_id, @p_replaced_by)", conn);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)tenantId);
        cmd.Parameters.AddWithValue("p_token_id", (Guid)tokenId);
        cmd.Parameters.AddWithValue("p_replaced_by", replacedBy.HasValue ? (object)(Guid)replacedBy.Value : DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
