using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Interfaces.Services;

public interface IPiiEncryptionService
{
    EncryptedField Encrypt(string plainText, UuidV7 tenantId);
    string Decrypt(EncryptedField field, UuidV7 tenantId);
    string HashForLookup(string value);
    Task DestroyKeyAsync(UuidV7 tenantId, CancellationToken ct = default);
}
