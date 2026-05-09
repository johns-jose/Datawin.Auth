using DataWin.Auth.Infrastructure.Tokens;
using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.UnitTests.Infrastructure;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _service = new(new JwtTokenSettings
    {
        SigningKey = "DataWin-Auth-Test-Signing-Key-256-Bits-Long!!",
        Issuer = "https://test.datawin.io",
        AccessTokenLifetimeSeconds = 300
    });

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwt()
    {
        var userId = UuidV7.New();
        var tenantId = UuidV7.New();

        var token = _service.GenerateAccessToken(userId, tenantId, "us-east-1", ["admin"], true, "test-app");

        Assert.NotEmpty(token);
        Assert.Contains(".", token);
    }

    [Fact]
    public async Task ValidateAccessToken_ValidToken_ReturnsSuccess()
    {
        var userId = UuidV7.New();
        var tenantId = UuidV7.New();

        var token = _service.GenerateAccessToken(userId, tenantId, "us-east-1", [], false, "test-app");
        var (isValid, parsedUserId, parsedTenantId) = await _service.ValidateAccessTokenAsync(token);

        Assert.True(isValid);
        Assert.Equal(userId, parsedUserId);
        Assert.Equal(tenantId, parsedTenantId);
    }

    [Fact]
    public async Task ValidateAccessToken_InvalidToken_ReturnsFalse()
    {
        var (isValid, _, _) = await _service.ValidateAccessTokenAsync("invalid.token.value");

        Assert.False(isValid);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnBase64String()
    {
        var token = _service.GenerateRefreshToken();

        Assert.NotEmpty(token);
        var bytes = Convert.FromBase64String(token);
        Assert.Equal(64, bytes.Length);
    }
}
