using DataWin.Auth.Infrastructure.Encryption;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DataWin.Auth.UnitTests.Infrastructure;

public class AesGcmFieldEncryptorTests
{
    [Fact]
    public void EncryptDecrypt_ShouldRoundTrip()
    {
        var keyService = Substitute.For<IKeyManagementService>();
        var tenantId = UuidV7.New();
        var key = new byte[32];
        Random.Shared.NextBytes(key);

        keyService.GetCurrentKey(Arg.Any<UuidV7>()).Returns((key, "key-1"));
        keyService.GetKeyById(Arg.Any<UuidV7>(), "key-1").Returns(key);

        var encryptor = new AesGcmFieldEncryptor(keyService, Substitute.For<ILogger<AesGcmFieldEncryptor>>());

        var plainText = "user@example.com";
        var encrypted = encryptor.Encrypt(plainText, tenantId);
        var decrypted = encryptor.Decrypt(encrypted, tenantId);

        Assert.Equal(plainText, decrypted);
        Assert.NotEmpty(encrypted.CipherText);
        Assert.Equal(12, encrypted.Nonce.Length);
        Assert.Equal(16, encrypted.Tag.Length);
    }

    [Fact]
    public void HashForLookup_ShouldBeConsistent()
    {
        var keyService = Substitute.For<IKeyManagementService>();
        var encryptor = new AesGcmFieldEncryptor(keyService, Substitute.For<ILogger<AesGcmFieldEncryptor>>());

        var hash1 = encryptor.HashForLookup("user@example.com");
        var hash2 = encryptor.HashForLookup("user@example.com");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashForLookup_DifferentInputs_DifferentHashes()
    {
        var keyService = Substitute.For<IKeyManagementService>();
        var encryptor = new AesGcmFieldEncryptor(keyService, Substitute.For<ILogger<AesGcmFieldEncryptor>>());

        var hash1 = encryptor.HashForLookup("user1@example.com");
        var hash2 = encryptor.HashForLookup("user2@example.com");

        Assert.NotEqual(hash1, hash2);
    }
}
