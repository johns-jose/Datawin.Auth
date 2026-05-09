using DataWin.Auth.Core.Entities;
using DataWin.Auth.Core.Interfaces;
using DataWin.Auth.Core.Interfaces.Repositories;
using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;
using Npgsql;

namespace DataWin.Auth.Infrastructure.Database.Repositories;

public sealed class OAuthClientRepository : IOAuthClientRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IRegionalRouter _router;

    public OAuthClientRepository(IDbConnectionFactory connectionFactory, IRegionalRouter router)
    {
        _connectionFactory = connectionFactory;
        _router = router;
    }

    public async Task<OAuthClient?> GetByClientIdAsync(UuidV7 tenantId, string clientId, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(tenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand(
            "SELECT * FROM oauth_clients WHERE tenant_id = @tid AND client_id = @cid AND is_active = true", conn);
        cmd.Parameters.AddWithValue("tid", (Guid)tenantId);
        cmd.Parameters.AddWithValue("cid", clientId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return new OAuthClient
        {
            Id = UuidV7.From(reader.GetGuid(reader.GetOrdinal("id"))),
            TenantId = UuidV7.From(reader.GetGuid(reader.GetOrdinal("tenant_id"))),
            ClientId = reader.GetString(reader.GetOrdinal("client_id")),
            ClientSecretHash = reader.GetString(reader.GetOrdinal("client_secret_hash")),
            DisplayName = reader.GetString(reader.GetOrdinal("display_name")),
            RedirectUris = (string[])reader.GetValue(reader.GetOrdinal("redirect_uris")),
            AllowedScopes = (string[])reader.GetValue(reader.GetOrdinal("allowed_scopes")),
            AllowedGrantTypes = (string[])reader.GetValue(reader.GetOrdinal("allowed_grant_types")),
            AccessTokenLifetimeSeconds = reader.GetInt32(reader.GetOrdinal("access_token_lifetime_seconds")),
            RefreshTokenLifetimeSeconds = reader.GetInt32(reader.GetOrdinal("refresh_token_lifetime_seconds")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at"))
        };
    }

    public async Task CreateAsync(OAuthClient client, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(client.TenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO oauth_clients (id, tenant_id, client_id, client_secret_hash, display_name, redirect_uris, allowed_scopes, allowed_grant_types, access_token_lifetime_seconds, refresh_token_lifetime_seconds, is_active, created_at, updated_at) VALUES (@id, @tid, @cid, @secret, @name, @uris, @scopes, @grants, @attl, @rttl, @active, @cat, @uat)", conn);
        cmd.Parameters.AddWithValue("id", (Guid)client.Id);
        cmd.Parameters.AddWithValue("tid", (Guid)client.TenantId);
        cmd.Parameters.AddWithValue("cid", client.ClientId);
        cmd.Parameters.AddWithValue("secret", client.ClientSecretHash);
        cmd.Parameters.AddWithValue("name", client.DisplayName);
        cmd.Parameters.AddWithValue("uris", client.RedirectUris);
        cmd.Parameters.AddWithValue("scopes", client.AllowedScopes);
        cmd.Parameters.AddWithValue("grants", client.AllowedGrantTypes);
        cmd.Parameters.AddWithValue("attl", client.AccessTokenLifetimeSeconds);
        cmd.Parameters.AddWithValue("rttl", client.RefreshTokenLifetimeSeconds);
        cmd.Parameters.AddWithValue("active", client.IsActive);
        cmd.Parameters.AddWithValue("cat", client.CreatedAt);
        cmd.Parameters.AddWithValue("uat", client.UpdatedAt);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateAsync(OAuthClient client, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(client.TenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand(
            "UPDATE oauth_clients SET display_name = @name, redirect_uris = @uris, allowed_scopes = @scopes, allowed_grant_types = @grants, is_active = @active, updated_at = @uat WHERE id = @id AND tenant_id = @tid", conn);
        cmd.Parameters.AddWithValue("id", (Guid)client.Id);
        cmd.Parameters.AddWithValue("tid", (Guid)client.TenantId);
        cmd.Parameters.AddWithValue("name", client.DisplayName);
        cmd.Parameters.AddWithValue("uris", client.RedirectUris);
        cmd.Parameters.AddWithValue("scopes", client.AllowedScopes);
        cmd.Parameters.AddWithValue("grants", client.AllowedGrantTypes);
        cmd.Parameters.AddWithValue("active", client.IsActive);
        cmd.Parameters.AddWithValue("uat", DateTimeOffset.UtcNow);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<SamlConfiguration?> GetSamlConfigAsync(UuidV7 tenantId, string entityId, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(tenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand(
            "SELECT * FROM saml_configurations WHERE tenant_id = @tid AND entity_id = @eid AND is_active = true", conn);
        cmd.Parameters.AddWithValue("tid", (Guid)tenantId);
        cmd.Parameters.AddWithValue("eid", entityId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return new SamlConfiguration
        {
            Id = UuidV7.From(reader.GetGuid(reader.GetOrdinal("id"))),
            TenantId = UuidV7.From(reader.GetGuid(reader.GetOrdinal("tenant_id"))),
            EntityId = reader.GetString(reader.GetOrdinal("entity_id")),
            MetadataUrl = reader.GetString(reader.GetOrdinal("metadata_url")),
            AssertionConsumerServiceUrl = reader.GetString(reader.GetOrdinal("assertion_consumer_service_url")),
            SingleLogoutServiceUrl = reader.GetString(reader.GetOrdinal("single_logout_service_url")),
            CertificateBase64 = reader.GetString(reader.GetOrdinal("certificate_base64")),
            SignRequests = reader.GetBoolean(reader.GetOrdinal("sign_requests")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at"))
        };
    }

    public async Task CreateSamlConfigAsync(SamlConfiguration config, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(config.TenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO saml_configurations (id, tenant_id, entity_id, metadata_url, assertion_consumer_service_url, single_logout_service_url, certificate_base64, sign_requests, is_active, created_at, updated_at) VALUES (@id, @tid, @eid, @meta, @acs, @slo, @cert, @sign, @active, @cat, @uat)", conn);
        cmd.Parameters.AddWithValue("id", (Guid)config.Id);
        cmd.Parameters.AddWithValue("tid", (Guid)config.TenantId);
        cmd.Parameters.AddWithValue("eid", config.EntityId);
        cmd.Parameters.AddWithValue("meta", config.MetadataUrl);
        cmd.Parameters.AddWithValue("acs", config.AssertionConsumerServiceUrl);
        cmd.Parameters.AddWithValue("slo", config.SingleLogoutServiceUrl);
        cmd.Parameters.AddWithValue("cert", config.CertificateBase64);
        cmd.Parameters.AddWithValue("sign", config.SignRequests);
        cmd.Parameters.AddWithValue("active", config.IsActive);
        cmd.Parameters.AddWithValue("cat", config.CreatedAt);
        cmd.Parameters.AddWithValue("uat", config.UpdatedAt);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateSamlConfigAsync(SamlConfiguration config, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(config.TenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand(
            "UPDATE saml_configurations SET metadata_url = @meta, assertion_consumer_service_url = @acs, single_logout_service_url = @slo, certificate_base64 = @cert, sign_requests = @sign, is_active = @active, updated_at = @uat WHERE id = @id AND tenant_id = @tid", conn);
        cmd.Parameters.AddWithValue("id", (Guid)config.Id);
        cmd.Parameters.AddWithValue("tid", (Guid)config.TenantId);
        cmd.Parameters.AddWithValue("meta", config.MetadataUrl);
        cmd.Parameters.AddWithValue("acs", config.AssertionConsumerServiceUrl);
        cmd.Parameters.AddWithValue("slo", config.SingleLogoutServiceUrl);
        cmd.Parameters.AddWithValue("cert", config.CertificateBase64);
        cmd.Parameters.AddWithValue("sign", config.SignRequests);
        cmd.Parameters.AddWithValue("active", config.IsActive);
        cmd.Parameters.AddWithValue("uat", DateTimeOffset.UtcNow);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
