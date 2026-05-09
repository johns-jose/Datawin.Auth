using System.Data;
using DataWin.Auth.Core.Entities;
using DataWin.Auth.Core.Interfaces;
using DataWin.Auth.Core.Interfaces.Repositories;
using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;
using Npgsql;

namespace DataWin.Auth.Infrastructure.Database.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IRegionalRouter _router;

    public UserRepository(IDbConnectionFactory connectionFactory, IRegionalRouter router)
    {
        _connectionFactory = connectionFactory;
        _router = router;
    }

    public async Task<User?> GetByIdAsync(UuidV7 tenantId, UuidV7 userId, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(tenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand("SELECT * FROM sp_user_get_by_id(@p_tenant_id, @p_user_id)", conn);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)tenantId);
        cmd.Parameters.AddWithValue("p_user_id", (Guid)userId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? MapUser(reader) : null;
    }

    public async Task<User?> GetByEmailHashAsync(UuidV7 tenantId, string emailHash, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(tenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand("SELECT * FROM sp_user_get_by_email_hash(@p_tenant_id, @p_email_hash)", conn);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)tenantId);
        cmd.Parameters.AddWithValue("p_email_hash", emailHash);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? MapUser(reader) : null;
    }

    public async Task CreateAsync(User user, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(user.TenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand(
            "CALL sp_user_create(@p_id, @p_tenant_id, @p_email_cipher, @p_email_nonce, @p_email_tag, @p_email_key_id, @p_email_hash, @p_display_name_cipher, @p_display_name_nonce, @p_display_name_tag, @p_display_name_key_id, @p_is_active, @p_email_confirmed, @p_mfa_enabled, @p_created_at, @p_updated_at)", conn);
        cmd.Parameters.AddWithValue("p_id", (Guid)user.Id);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)user.TenantId);
        cmd.Parameters.AddWithValue("p_email_cipher", user.Email.CipherText);
        cmd.Parameters.AddWithValue("p_email_nonce", user.Email.Nonce);
        cmd.Parameters.AddWithValue("p_email_tag", user.Email.Tag);
        cmd.Parameters.AddWithValue("p_email_key_id", user.Email.KeyId);
        cmd.Parameters.AddWithValue("p_email_hash", user.EmailHash);
        cmd.Parameters.AddWithValue("p_display_name_cipher", (object?)user.DisplayName?.CipherText ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_display_name_nonce", (object?)user.DisplayName?.Nonce ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_display_name_tag", (object?)user.DisplayName?.Tag ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_display_name_key_id", (object?)user.DisplayName?.KeyId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_is_active", user.IsActive);
        cmd.Parameters.AddWithValue("p_email_confirmed", user.EmailConfirmed);
        cmd.Parameters.AddWithValue("p_mfa_enabled", user.MfaEnabled);
        cmd.Parameters.AddWithValue("p_created_at", user.CreatedAt);
        cmd.Parameters.AddWithValue("p_updated_at", user.UpdatedAt);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(user.TenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand(
            "CALL sp_user_update(@p_id, @p_tenant_id, @p_is_active, @p_email_confirmed, @p_mfa_enabled, @p_failed_login_attempts, @p_lockout_end, @p_updated_at)", conn);
        cmd.Parameters.AddWithValue("p_id", (Guid)user.Id);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)user.TenantId);
        cmd.Parameters.AddWithValue("p_is_active", user.IsActive);
        cmd.Parameters.AddWithValue("p_email_confirmed", user.EmailConfirmed);
        cmd.Parameters.AddWithValue("p_mfa_enabled", user.MfaEnabled);
        cmd.Parameters.AddWithValue("p_failed_login_attempts", user.FailedLoginAttempts);
        cmd.Parameters.AddWithValue("p_lockout_end", (object?)user.LockoutEnd ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_updated_at", DateTimeOffset.UtcNow);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<UserCredential?> GetCredentialAsync(UuidV7 tenantId, UuidV7 userId, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(tenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand("SELECT * FROM sp_user_get_credential(@p_tenant_id, @p_user_id)", conn);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)tenantId);
        cmd.Parameters.AddWithValue("p_user_id", (Guid)userId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return new UserCredential
        {
            Id = UuidV7.From(reader.GetGuid(reader.GetOrdinal("id"))),
            UserId = UuidV7.From(reader.GetGuid(reader.GetOrdinal("user_id"))),
            TenantId = UuidV7.From(reader.GetGuid(reader.GetOrdinal("tenant_id"))),
            PasswordHash = new HashedPassword
            {
                Hash = reader.GetString(reader.GetOrdinal("password_hash")),
                Algorithm = reader.GetString(reader.GetOrdinal("algorithm"))
            },
            RecoveryCodes = reader.IsDBNull(reader.GetOrdinal("recovery_codes")) ? null : reader.GetString(reader.GetOrdinal("recovery_codes")),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at"))
        };
    }

    public async Task CreateCredentialAsync(UserCredential credential, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(credential.TenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand(
            "CALL sp_user_create_credential(@p_id, @p_user_id, @p_tenant_id, @p_password_hash, @p_algorithm, @p_recovery_codes, @p_created_at, @p_updated_at)", conn);
        cmd.Parameters.AddWithValue("p_id", (Guid)credential.Id);
        cmd.Parameters.AddWithValue("p_user_id", (Guid)credential.UserId);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)credential.TenantId);
        cmd.Parameters.AddWithValue("p_password_hash", credential.PasswordHash.Hash);
        cmd.Parameters.AddWithValue("p_algorithm", credential.PasswordHash.Algorithm);
        cmd.Parameters.AddWithValue("p_recovery_codes", (object?)credential.RecoveryCodes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_created_at", credential.CreatedAt);
        cmd.Parameters.AddWithValue("p_updated_at", credential.UpdatedAt);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateCredentialAsync(UserCredential credential, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(credential.TenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand(
            "CALL sp_user_update_credential(@p_id, @p_tenant_id, @p_password_hash, @p_algorithm, @p_recovery_codes, @p_updated_at)", conn);
        cmd.Parameters.AddWithValue("p_id", (Guid)credential.Id);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)credential.TenantId);
        cmd.Parameters.AddWithValue("p_password_hash", credential.PasswordHash.Hash);
        cmd.Parameters.AddWithValue("p_algorithm", credential.PasswordHash.Algorithm);
        cmd.Parameters.AddWithValue("p_recovery_codes", (object?)credential.RecoveryCodes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_updated_at", DateTimeOffset.UtcNow);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<UserExternalLogin>> GetExternalLoginsAsync(UuidV7 tenantId, UuidV7 userId, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(tenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand("SELECT * FROM sp_user_get_external_logins(@p_tenant_id, @p_user_id)", conn);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)tenantId);
        cmd.Parameters.AddWithValue("p_user_id", (Guid)userId);

        var logins = new List<UserExternalLogin>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            logins.Add(new UserExternalLogin
            {
                Id = UuidV7.From(reader.GetGuid(reader.GetOrdinal("id"))),
                UserId = UuidV7.From(reader.GetGuid(reader.GetOrdinal("user_id"))),
                TenantId = UuidV7.From(reader.GetGuid(reader.GetOrdinal("tenant_id"))),
                Provider = (Core.Enums.AuthSchemeType)reader.GetInt32(reader.GetOrdinal("provider")),
                ProviderKey = reader.GetString(reader.GetOrdinal("provider_key")),
                ProviderDisplayName = reader.IsDBNull(reader.GetOrdinal("provider_display_name")) ? null : reader.GetString(reader.GetOrdinal("provider_display_name")),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at"))
            });
        }
        return logins;
    }

    public async Task LinkExternalLoginAsync(UserExternalLogin login, CancellationToken ct = default)
    {
        var region = await _router.ResolvePrimaryRegionAsync(login.TenantId, ct);
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateRegionalConnectionAsync(region, ct);
        await using var cmd = new NpgsqlCommand(
            "CALL sp_user_link_external_login(@p_id, @p_user_id, @p_tenant_id, @p_provider, @p_provider_key, @p_provider_display_name, @p_created_at)", conn);
        cmd.Parameters.AddWithValue("p_id", (Guid)login.Id);
        cmd.Parameters.AddWithValue("p_user_id", (Guid)login.UserId);
        cmd.Parameters.AddWithValue("p_tenant_id", (Guid)login.TenantId);
        cmd.Parameters.AddWithValue("p_provider", (int)login.Provider);
        cmd.Parameters.AddWithValue("p_provider_key", login.ProviderKey);
        cmd.Parameters.AddWithValue("p_provider_display_name", (object?)login.ProviderDisplayName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_created_at", login.CreatedAt);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static User MapUser(NpgsqlDataReader reader) => new()
    {
        Id = UuidV7.From(reader.GetGuid(reader.GetOrdinal("id"))),
        TenantId = UuidV7.From(reader.GetGuid(reader.GetOrdinal("tenant_id"))),
        Email = EncryptedField.FromComponents(
            (byte[])reader["email_cipher"],
            (byte[])reader["email_nonce"],
            (byte[])reader["email_tag"],
            reader.GetString(reader.GetOrdinal("email_key_id"))),
        EmailHash = reader.GetString(reader.GetOrdinal("email_hash")),
        DisplayName = reader.IsDBNull(reader.GetOrdinal("display_name_cipher")) ? null : EncryptedField.FromComponents(
            (byte[])reader["display_name_cipher"],
            (byte[])reader["display_name_nonce"],
            (byte[])reader["display_name_tag"],
            reader.GetString(reader.GetOrdinal("display_name_key_id"))),
        IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
        EmailConfirmed = reader.GetBoolean(reader.GetOrdinal("email_confirmed")),
        MfaEnabled = reader.GetBoolean(reader.GetOrdinal("mfa_enabled")),
        FailedLoginAttempts = reader.GetInt32(reader.GetOrdinal("failed_login_attempts")),
        LockoutEnd = reader.IsDBNull(reader.GetOrdinal("lockout_end")) ? null : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("lockout_end")),
        CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
        UpdatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at"))
    };
}
