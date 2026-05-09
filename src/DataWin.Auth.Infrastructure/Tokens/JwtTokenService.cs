using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.IdentityModel.Tokens;

namespace DataWin.Auth.Infrastructure.Tokens;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtTokenSettings _settings;

    public JwtTokenService(JwtTokenSettings settings) => _settings = settings;

    public string GenerateAccessToken(UuidV7 userId, UuidV7 tenantId, string region, IReadOnlyList<string> roles, bool mfaVerified, string audience)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("tid", tenantId.ToString()),
            new("region", region),
            new("mfa_verified", mfaVerified.ToString().ToLowerInvariant())
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(_settings.AccessTokenLifetimeSeconds),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public Task<(bool IsValid, UuidV7 UserId, UuidV7 TenantId)> ValidateAccessTokenAsync(string token, CancellationToken ct = default)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        tokenHandler.InboundClaimTypeMap.Clear();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _settings.Issuer,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            }, out _);

            var userId = UuidV7.Parse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value!);
            var tenantId = UuidV7.Parse(principal.FindFirst("tid")?.Value!);

            return Task.FromResult((true, userId, tenantId));
        }
        catch
        {
            return Task.FromResult((false, UuidV7.Empty, UuidV7.Empty));
        }
    }
}

public sealed record JwtTokenSettings
{
    public required string SigningKey { get; init; }
    public required string Issuer { get; init; }
    public int AccessTokenLifetimeSeconds { get; init; } = 900;
}


