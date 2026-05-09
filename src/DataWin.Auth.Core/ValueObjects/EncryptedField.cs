namespace DataWin.Auth.Core.ValueObjects;

public sealed record EncryptedField
{
    public required byte[] CipherText { get; init; }
    public required byte[] Nonce { get; init; }
    public required byte[] Tag { get; init; }
    public required string KeyId { get; init; }

    public static EncryptedField FromComponents(byte[] cipherText, byte[] nonce, byte[] tag, string keyId)
        => new() { CipherText = cipherText, Nonce = nonce, Tag = tag, KeyId = keyId };
}
