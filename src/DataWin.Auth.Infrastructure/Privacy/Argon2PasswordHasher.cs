using System.Security.Cryptography;
using System.Text;
using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DataWin.Auth.Infrastructure.Privacy;

public sealed class Argon2PasswordHasher : IPasswordHasher
{
    // Using PBKDF2 as a stand-in; in production use Konscious.Security.Cryptography for Argon2id
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    private readonly ILogger<Argon2PasswordHasher> _logger;

    public Argon2PasswordHasher(ILogger<Argon2PasswordHasher> logger)
    {
        _logger = logger;
    }

    public HashedPassword Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, HashSize);

        var combined = $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        return new HashedPassword { Hash = combined, Algorithm = HashedPassword.Argon2Id };
    }

    public bool Verify(string password, HashedPassword hashedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword.Hash))
        {
            _logger.LogWarning("Password verification failed — stored hash is null or empty");
            return false;
        }

        var parts = hashedPassword.Hash.Split('.');
        if (parts.Length != 2)
        {
            _logger.LogWarning("Password verification failed — expected format 'base64(salt).base64(hash)' but got {PartCount} segment(s) (length={TotalLength})",
                parts.Length, hashedPassword.Hash.Length);
            return false;
        }

        byte[] salt;
        byte[] storedHash;
        try
        {
            salt = Convert.FromBase64String(parts[0]);
            storedHash = Convert.FromBase64String(parts[1]);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Password verification failed — stored hash contains invalid base64");
            return false;
        }

        if (salt.Length != SaltSize)
        {
            _logger.LogWarning("Password verification failed — salt length {ActualSaltLength} does not match expected {ExpectedSaltLength}",
                salt.Length, SaltSize);
            return false;
        }

        if (storedHash.Length != HashSize)
        {
            _logger.LogWarning("Password verification failed — hash length {ActualHashLength} does not match expected {ExpectedHashLength}",
                storedHash.Length, HashSize);
            return false;
        }

        var computedHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, HashSize);

        var isValid = CryptographicOperations.FixedTimeEquals(computedHash, storedHash);

        if (!isValid)
        {
            _logger.LogDebug("Password verification failed — computed hash does not match stored hash (algorithm={Algorithm})",
                hashedPassword.Algorithm);
        }

        return isValid;
    }
}
