using DataWin.Auth.Core.Entities;
using DataWin.Auth.Core.Interfaces;
using DataWin.Auth.Core.Interfaces.Repositories;
using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;
using Npgsql;

namespace DataWin.Auth.Infrastructure.Database.Repositories;

public sealed class PiiAuditRepository : IPiiAuditRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IRegionalRouter _router;

    public PiiAuditRepository(IDbConnectionFactory connectionFactory, IRegionalRouter router)
    {
        _connectionFactory = connectionFactory;
        _router = router;
    }

    public async Task WriteAsync(PiiAuditEntry entry, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(entry.TenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand(
            "CALL sp_pii_audit_write(@p_id, @p_tenant_id, @p_user_id, @p_actor_id, @p_action, @p_field_name, @p_reason, @p_ip_address, @p_timestamp)", conn);
        cmd.Parameters.AddWithValue("p_id", (Guid)entry.Id);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)entry.TenantId);
        cmd.Parameters.AddWithValue("p_user_id", (Guid)entry.UserId);
        cmd.Parameters.AddWithValue("p_actor_id", entry.ActorId.HasValue ? (object)(Guid)entry.ActorId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("p_action", entry.Action);
        cmd.Parameters.AddWithValue("p_field_name", entry.FieldName);
        cmd.Parameters.AddWithValue("p_reason", entry.Reason);
        cmd.Parameters.AddWithValue("p_ip_address", entry.IpAddress);
        cmd.Parameters.AddWithValue("p_timestamp", entry.Timestamp);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<PiiAuditEntry>> GetByUserAsync(UuidV7 tenantId, UuidV7 userId, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(tenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand("SELECT * FROM sp_pii_audit_get_by_user(@p_tenant_id, @p_user_id)", conn);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)tenantId);
        cmd.Parameters.AddWithValue("p_user_id", (Guid)userId);

        var entries = new List<PiiAuditEntry>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            entries.Add(new PiiAuditEntry
            {
                Id = UuidV7.From(reader.GetGuid(reader.GetOrdinal("id"))),
                TenantId = UuidV7.From(reader.GetGuid(reader.GetOrdinal("tenant_id"))),
                UserId = UuidV7.From(reader.GetGuid(reader.GetOrdinal("user_id"))),
                ActorId = reader.IsDBNull(reader.GetOrdinal("actor_id")) ? null : UuidV7.From(reader.GetGuid(reader.GetOrdinal("actor_id"))),
                Action = reader.GetString(reader.GetOrdinal("action")),
                FieldName = reader.GetString(reader.GetOrdinal("field_name")),
                Reason = reader.GetString(reader.GetOrdinal("reason")),
                IpAddress = reader.GetString(reader.GetOrdinal("ip_address")),
                Timestamp = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("timestamp"))
            });
        }
        return entries;
    }
}
