using System.Security.Cryptography;
using System.Text;
using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DataWin.Auth.Infrastructure.Encryption;

public sealed class AesGcmFieldEncryptor : IPiiEncryptionService
{
    private readonly IKeyManagementService _keyService;
    private readonly ILogger<AesGcmFieldEncryptor> _logger;

    public AesGcmFieldEncryptor(IKeyManagementService keyService, ILogger<AesGcmFieldEncryptor> logger)
    {
        _keyService = keyService;
        _logger = logger;
    }

    public EncryptedField Encrypt(string plainText, UuidV7 tenantId)
    {
        var (key, keyId) = _keyService.GetCurrentKey(tenantId);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);
        var cipherText = new byte[plainBytes.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(key, tagSizeInBytes: 16);
        aes.Encrypt(nonce, plainBytes, cipherText, tag);

        return EncryptedField.FromComponents(cipherText, nonce, tag, keyId);
    }

    public string Decrypt(EncryptedField field, UuidV7 tenantId)
    {
        var key = _keyService.GetKeyById(tenantId, field.KeyId);
        var plainBytes = new byte[field.CipherText.Length];

        using var aes = new AesGcm(key, tagSizeInBytes: 16);
        aes.Decrypt(field.Nonce, field.CipherText, field.Tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    public string HashForLookup(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value.ToLowerInvariant()));
        return Convert.ToBase64String(bytes);
    }

    public async Task DestroyKeyAsync(UuidV7 tenantId, CancellationToken ct = default)
    {
        await _keyService.DestroyKeyAsync(tenantId, ct);
        _logger.LogWarning("Encryption key destroyed for tenant {TenantId} — crypto-shredding complete", tenantId);
    }
}

public interface IKeyManagementService
{
    (byte[] Key, string KeyId) GetCurrentKey(UuidV7 tenantId);
    byte[] GetKeyById(UuidV7 tenantId, string keyId);
    Task DestroyKeyAsync(UuidV7 tenantId, CancellationToken ct = default);
}
