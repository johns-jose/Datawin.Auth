using System.Data;
using DataWin.Auth.Core.Entities;
using DataWin.Auth.Core.Enums;
using DataWin.Auth.Core.Interfaces;
using DataWin.Auth.Core.Interfaces.Repositories;
using DataWin.Auth.Core.ValueObjects;
using Npgsql;
using NpgsqlTypes;

namespace DataWin.Auth.Infrastructure.Database.Repositories;

public sealed class TenantRepository : ITenantRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TenantRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<Tenant?> GetByIdAsync(UuidV7 id, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateGlobalConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand("SELECT * FROM sp_tenant_get_by_id(@p_id)", conn);
        cmd.Parameters.AddWithValue("p_id", (Guid)id);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? MapTenant(reader) : null;
    }

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateGlobalConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand("SELECT * FROM sp_tenant_get_by_slug(@p_slug)", conn);
        cmd.Parameters.AddWithValue("p_slug", slug);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? MapTenant(reader) : null;
    }

    public async Task<Tenant?> GetByDomainAsync(string domain, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateGlobalConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand("SELECT * FROM sp_tenant_get_by_domain(@p_domain)", conn);
        cmd.Parameters.AddWithValue("p_domain", domain);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? MapTenant(reader) : null;
    }

    public async Task CreateAsync(Tenant tenant, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateGlobalConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand("CALL sp_tenant_create(@p_id, @p_name, @p_slug, @p_domain, @p_status, @p_created_at, @p_updated_at)", conn);
        cmd.Parameters.AddWithValue("p_id", (Guid)tenant.Id);
        cmd.Parameters.AddWithValue("p_name", tenant.Name);
        cmd.Parameters.AddWithValue("p_slug", tenant.Slug);
        cmd.Parameters.AddWithValue("p_domain", (object?)tenant.Domain ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_status", (int)tenant.Status);
        cmd.Parameters.AddWithValue("p_created_at", tenant.CreatedAt);
        cmd.Parameters.AddWithValue("p_updated_at", tenant.UpdatedAt);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateAsync(Tenant tenant, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateGlobalConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand("CALL sp_tenant_update(@p_id, @p_name, @p_slug, @p_domain, @p_status, @p_updated_at)", conn);
        cmd.Parameters.AddWithValue("p_id", (Guid)tenant.Id);
        cmd.Parameters.AddWithValue("p_name", tenant.Name);
        cmd.Parameters.AddWithValue("p_slug", tenant.Slug);
        cmd.Parameters.AddWithValue("p_domain", (object?)tenant.Domain ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_status", (int)tenant.Status);
        cmd.Parameters.AddWithValue("p_updated_at", DateTimeOffset.UtcNow);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<TenantRegion>> GetRegionsAsync(UuidV7 tenantId, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateGlobalConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand("SELECT * FROM sp_tenant_get_regions(@p_tenant_id)", conn);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)tenantId);

        var regions = new List<TenantRegion>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            regions.Add(new TenantRegion
            {
                Id = UuidV7.From(reader.GetGuid(reader.GetOrdinal("id"))),
                TenantId = UuidV7.From(reader.GetGuid(reader.GetOrdinal("tenant_id"))),
                RegionCode = new RegionCode(reader.GetString(reader.GetOrdinal("region_code"))),
                IsPrimary = reader.GetBoolean(reader.GetOrdinal("is_primary")),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at"))
            });
        }
        return regions;
    }

    public async Task AddRegionAsync(TenantRegion region, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateGlobalConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand("CALL sp_tenant_add_region(@p_id, @p_tenant_id, @p_region_code, @p_is_primary, @p_created_at)", conn);
        cmd.Parameters.AddWithValue("p_id", (Guid)region.Id);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)region.TenantId);
        cmd.Parameters.AddWithValue("p_region_code", region.RegionCode.Value);
        cmd.Parameters.AddWithValue("p_is_primary", region.IsPrimary);
        cmd.Parameters.AddWithValue("p_created_at", region.CreatedAt);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task RemoveRegionAsync(UuidV7 tenantId, RegionCode regionCode, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateGlobalConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand("CALL sp_tenant_remove_region(@p_tenant_id, @p_region_code)", conn);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)tenantId);
        cmd.Parameters.AddWithValue("p_region_code", regionCode.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static Tenant MapTenant(NpgsqlDataReader reader) => new()
    {
        Id = UuidV7.From(reader.GetGuid(reader.GetOrdinal("id"))),
        Name = reader.GetString(reader.GetOrdinal("name")),
        Slug = reader.GetString(reader.GetOrdinal("slug")),
        Domain = reader.IsDBNull(reader.GetOrdinal("domain")) ? null : reader.GetString(reader.GetOrdinal("domain")),
        Status = (TenantStatus)reader.GetInt32(reader.GetOrdinal("status")),
        CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
        UpdatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at"))
    };
}
