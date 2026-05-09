using DataWin.Auth.Core.Entities;
using DataWin.Auth.Core.Enums;
using DataWin.Auth.Core.Interfaces;
using DataWin.Auth.Core.Interfaces.Repositories;
using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;
using Npgsql;

namespace DataWin.Auth.Infrastructure.Database.Repositories;

public sealed class ConsentRepository : IConsentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IRegionalRouter _router;

    public ConsentRepository(IDbConnectionFactory connectionFactory, IRegionalRouter router)
    {
        _connectionFactory = connectionFactory;
        _router = router;
    }

    public async Task<ConsentRecord?> GetAsync(UuidV7 tenantId, UuidV7 userId, ConsentPurpose purpose, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(tenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand("SELECT * FROM sp_consent_get(@p_tenant_id, @p_user_id, @p_purpose)", conn);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)tenantId);
        cmd.Parameters.AddWithValue("p_user_id", (Guid)userId);
        cmd.Parameters.AddWithValue("p_purpose", (int)purpose);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? MapConsent(reader) : null;
    }

    public async Task<IReadOnlyList<ConsentRecord>> GetAllForUserAsync(UuidV7 tenantId, UuidV7 userId, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(tenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand("SELECT * FROM sp_consent_get_all_for_user(@p_tenant_id, @p_user_id)", conn);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)tenantId);
        cmd.Parameters.AddWithValue("p_user_id", (Guid)userId);

        var records = new List<ConsentRecord>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            records.Add(MapConsent(reader));
        return records;
    }

    public async Task GrantAsync(ConsentRecord record, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(record.TenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand(
            "CALL sp_consent_grant(@p_id, @p_user_id, @p_tenant_id, @p_purpose, @p_ip_address, @p_granted_at)", conn);
        cmd.Parameters.AddWithValue("p_id", (Guid)record.Id);
        cmd.Parameters.AddWithValue("p_user_id", (Guid)record.UserId);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)record.TenantId);
        cmd.Parameters.AddWithValue("p_purpose", (int)record.Purpose);
        cmd.Parameters.AddWithValue("p_ip_address", record.IpAddress);
        cmd.Parameters.AddWithValue("p_granted_at", record.GrantedAt);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task WithdrawAsync(UuidV7 tenantId, UuidV7 userId, ConsentPurpose purpose, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(tenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand("CALL sp_consent_withdraw(@p_tenant_id, @p_user_id, @p_purpose)", conn);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)tenantId);
        cmd.Parameters.AddWithValue("p_user_id", (Guid)userId);
        cmd.Parameters.AddWithValue("p_purpose", (int)purpose);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<DataErasureRequest?> GetErasureRequestAsync(UuidV7 tenantId, UuidV7 requestId, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(tenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand("SELECT * FROM sp_erasure_get(@p_tenant_id, @p_request_id)", conn);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)tenantId);
        cmd.Parameters.AddWithValue("p_request_id", (Guid)requestId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return new DataErasureRequest
        {
            Id = UuidV7.From(reader.GetGuid(reader.GetOrdinal("id"))),
            UserId = UuidV7.From(reader.GetGuid(reader.GetOrdinal("user_id"))),
            TenantId = UuidV7.From(reader.GetGuid(reader.GetOrdinal("tenant_id"))),
            Status = (ErasureStatus)reader.GetInt32(reader.GetOrdinal("status")),
            RequestedByIp = reader.GetString(reader.GetOrdinal("requested_by_ip")),
            CompletionNotes = reader.IsDBNull(reader.GetOrdinal("completion_notes")) ? null : reader.GetString(reader.GetOrdinal("completion_notes")),
            RequestedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("requested_at")),
            CompletedAt = reader.IsDBNull(reader.GetOrdinal("completed_at")) ? null : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("completed_at"))
        };
    }

    public async Task CreateErasureRequestAsync(DataErasureRequest request, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(request.TenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand(
            "CALL sp_erasure_request_create(@p_id, @p_user_id, @p_tenant_id, @p_status, @p_requested_by_ip, @p_requested_at)", conn);
        cmd.Parameters.AddWithValue("p_id", (Guid)request.Id);
        cmd.Parameters.AddWithValue("p_user_id", (Guid)request.UserId);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)request.TenantId);
        cmd.Parameters.AddWithValue("p_status", (int)request.Status);
        cmd.Parameters.AddWithValue("p_requested_by_ip", request.RequestedByIp);
        cmd.Parameters.AddWithValue("p_requested_at", request.RequestedAt);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateErasureRequestAsync(DataErasureRequest request, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(request.TenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand(
            "CALL sp_erasure_request_update(@p_id, @p_tenant_id, @p_status, @p_completion_notes, @p_completed_at)", conn);
        cmd.Parameters.AddWithValue("p_id", (Guid)request.Id);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)request.TenantId);
        cmd.Parameters.AddWithValue("p_status", (int)request.Status);
        cmd.Parameters.AddWithValue("p_completion_notes", (object?)request.CompletionNotes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_completed_at", (object?)request.CompletedAt ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static ConsentRecord MapConsent(NpgsqlDataReader reader) => new()
    {
        Id = UuidV7.From(reader.GetGuid(reader.GetOrdinal("id"))),
        UserId = UuidV7.From(reader.GetGuid(reader.GetOrdinal("user_id"))),
        TenantId = UuidV7.From(reader.GetGuid(reader.GetOrdinal("tenant_id"))),
        Purpose = (ConsentPurpose)reader.GetInt32(reader.GetOrdinal("purpose")),
        IsGranted = reader.GetBoolean(reader.GetOrdinal("is_granted")),
        IpAddress = reader.GetString(reader.GetOrdinal("ip_address")),
        GrantedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("granted_at")),
        WithdrawnAt = reader.IsDBNull(reader.GetOrdinal("withdrawn_at")) ? null : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("withdrawn_at"))
    };
}
