using System.Security.Cryptography;
using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Infrastructure.Identity.Mfa;

public sealed class TotpProvider : IMfaProvider
{
    private readonly Core.Enums.MfaMethod _method = Core.Enums.MfaMethod.Totp;
    public Core.Enums.MfaMethod Method => _method;

    private const int TimeStepSeconds = 30;
    private const int CodeDigits = 6;

    public Task<string> EnrollAsync(UuidV7 tenantId, UuidV7 userId, CancellationToken ct = default)
    {
        var secret = new byte[20];
        RandomNumberGenerator.Fill(secret);
        var base32Secret = Base32Encode(secret);
        // Return provisioning URI for authenticator apps
        var uri = $"otpauth://totp/DataWin:{userId}?secret={base32Secret}&issuer=DataWin&digits={CodeDigits}&period={TimeStepSeconds}";
        return Task.FromResult(uri);
    }

    public Task<bool> VerifyAsync(UuidV7 tenantId, UuidV7 userId, string code, CancellationToken ct = default)
    {
        // In production: retrieve stored secret from DB and verify TOTP
        // This is a skeleton — real implementation needs the stored secret
        if (string.IsNullOrWhiteSpace(code) || code.Length != CodeDigits)
            return Task.FromResult(false);

        // Placeholder: actual TOTP verification against stored secret
        return Task.FromResult(true);
    }

    public Task DisableAsync(UuidV7 tenantId, UuidV7 userId, CancellationToken ct = default)
    {
        // Remove MFA enrollment from DB via stored procedure
        return Task.CompletedTask;
    }

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var result = new char[(data.Length * 8 + 4) / 5];
        int buffer = data[0], next = 1, bitsLeft = 8, index = 0;

        while (index < result.Length)
        {
            if (bitsLeft < 5)
            {
                if (next < data.Length)
                {
                    buffer <<= 8;
                    buffer |= data[next++];
                    bitsLeft += 8;
                }
                else
                {
                    int pad = 5 - bitsLeft;
                    buffer <<= pad;
                    bitsLeft += pad;
                }
            }
            result[index++] = alphabet[(buffer >> (bitsLeft - 5)) & 0x1F];
            bitsLeft -= 5;
        }
        return new string(result);
    }
}
