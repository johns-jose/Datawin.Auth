using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.UnitTests.Core;

public class ValueObjectTests
{
    [Fact]
    public void RegionCode_ShouldNormalize()
    {
        var region = new RegionCode("US-East-1");
        Assert.Equal("us-east-1", region.Value);
    }

    [Fact]
    public void RegionCode_ShouldThrowOnEmpty()
    {
        Assert.Throws<ArgumentException>(() => new RegionCode(""));
    }

    [Fact]
    public void TenantId_New_ShouldCreateNonEmpty()
    {
        var id = TenantId.New();
        Assert.False(id.IsEmpty);
    }

    [Fact]
    public void TenantId_Empty_ShouldBeEmpty()
    {
        var id = TenantId.Empty;
        Assert.True(id.IsEmpty);
    }

    [Fact]
    public void EncryptedField_ShouldStoreComponents()
    {
        var field = EncryptedField.FromComponents(
            new byte[] { 1, 2, 3 },
            new byte[] { 4, 5, 6 },
            new byte[] { 7, 8, 9 },
            "key-123");

        Assert.Equal(new byte[] { 1, 2, 3 }, field.CipherText);
        Assert.Equal(new byte[] { 4, 5, 6 }, field.Nonce);
        Assert.Equal(new byte[] { 7, 8, 9 }, field.Tag);
        Assert.Equal("key-123", field.KeyId);
    }

    [Fact]
    public void HashedPassword_ShouldStoreValues()
    {
        var hash = new HashedPassword { Hash = "abc", Algorithm = HashedPassword.Argon2Id };
        Assert.Equal("abc", hash.Hash);
        Assert.Equal("argon2id", hash.Algorithm);
    }
}
