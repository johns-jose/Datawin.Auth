using DataWin.Auth.Infrastructure.Privacy;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DataWin.Auth.UnitTests.Infrastructure;

public class PasswordHasherTests
{
    private readonly Argon2PasswordHasher _hasher = new(Substitute.For<ILogger<Argon2PasswordHasher>>());

    [Fact]
    public void Hash_ShouldReturnNonEmptyHash()
    {
        var result = _hasher.Hash("password123");

        Assert.NotNull(result.Hash);
        Assert.NotEmpty(result.Hash);
        Assert.Equal(HashedPassword.Argon2Id, result.Algorithm);
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("password123");
        Assert.True(_hasher.Verify("password123", hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("password123");
        Assert.False(_hasher.Verify("wrongpassword", hash));
    }

    [Fact]
    public void Hash_DifferentPasswords_ProduceDifferentHashes()
    {
        var hash1 = _hasher.Hash("password1");
        var hash2 = _hasher.Hash("password2");

        Assert.NotEqual(hash1.Hash, hash2.Hash);
    }

    [Fact]
    public void Hash_SamePassword_ProducesDifferentHashes()
    {
        var hash1 = _hasher.Hash("password");
        var hash2 = _hasher.Hash("password");

        Assert.NotEqual(hash1.Hash, hash2.Hash);
        Assert.True(_hasher.Verify("password", hash1));
        Assert.True(_hasher.Verify("password", hash2));
    }

    [Fact]
    public void Verify_SimulatedDbRoundTrip_ReturnsTrue()
    {
        // Simulate: Hash → store as string → retrieve string → Verify
        var original = _hasher.Hash("MyP@ssw0rd!");

        // Simulate the database round-trip: stored as TEXT, read back as string
        var fromDb = new HashedPassword
        {
            Hash = original.Hash,
            Algorithm = original.Algorithm
        };

        Assert.True(_hasher.Verify("MyP@ssw0rd!", fromDb));
    }

    [Fact]
    public void Verify_EmptyHash_ReturnsFalse()
    {
        var hash = new HashedPassword { Hash = "", Algorithm = HashedPassword.Argon2Id };
        Assert.False(_hasher.Verify("password", hash));
    }

    [Fact]
    public void Verify_InvalidBase64_ReturnsFalse()
    {
        var hash = new HashedPassword { Hash = "not-valid-base64.also-not-valid", Algorithm = HashedPassword.Argon2Id };
        Assert.False(_hasher.Verify("password", hash));
    }

    [Fact]
    public void Verify_WrongSaltLength_ReturnsFalse()
    {
        // Use a 8-byte salt instead of 16
        var shortSalt = Convert.ToBase64String(new byte[8]);
        var fakeHash = Convert.ToBase64String(new byte[32]);
        var hash = new HashedPassword { Hash = $"{shortSalt}.{fakeHash}", Algorithm = HashedPassword.Argon2Id };

        Assert.False(_hasher.Verify("password", hash));
    }

    [Fact]
    public void Verify_WrongHashLength_ReturnsFalse()
    {
        // Use a 16-byte hash instead of 32
        var validSalt = Convert.ToBase64String(new byte[16]);
        var shortHash = Convert.ToBase64String(new byte[16]);
        var hash = new HashedPassword { Hash = $"{validSalt}.{shortHash}", Algorithm = HashedPassword.Argon2Id };

        Assert.False(_hasher.Verify("password", hash));
    }

    [Fact]
    public void Verify_TruncatedHash_ReturnsFalse()
    {
        // Simulate a hash that was truncated (e.g., stored in a VARCHAR column too short)
        var original = _hasher.Hash("password");
        var truncatedHash = new HashedPassword
        {
            Hash = original.Hash[..^5],
            Algorithm = original.Algorithm
        };

        Assert.False(_hasher.Verify("password", truncatedHash));
    }

    [Fact]
    public void Hash_Format_IsTwoBase64SegmentsSeparatedByDot()
    {
        var result = _hasher.Hash("password");
        var parts = result.Hash.Split('.');

        Assert.Equal(2, parts.Length);
        Assert.Equal(16, Convert.FromBase64String(parts[0]).Length); // salt = 16 bytes
        Assert.Equal(32, Convert.FromBase64String(parts[1]).Length); // hash = 32 bytes
    }
}
