using DataWin.Auth.Core.Enums;
using DataWin.Auth.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace DataWin.Auth.Infrastructure.Identity.OpenIdConnect;

public sealed class OidcScheme : IAuthenticationScheme
{
    private readonly ILogger<OidcScheme> _logger;

    public OidcScheme(ILogger<OidcScheme> logger) => _logger = logger;

    public AuthSchemeType SchemeType => AuthSchemeType.OpenIdConnect;

    public async Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return AuthenticationResult.Failure("Authorization code is required.");

        // 1. Load OIDC discovery document for tenant (.well-known/openid-configuration)
        // 2. Exchange code at token endpoint
        // 3. Validate ID token (signature via JWK set, issuer, audience, nonce)
        // 4. Call UserInfo endpoint if needed
        // 5. Map claims to user identity

        _logger.LogInformation("OIDC authentication for tenant {Tenant}", request.TenantSlug);

        await Task.CompletedTask;
        return AuthenticationResult.Failure("OIDC code exchange not yet implemented. Configure tenant OIDC discovery URL.");
    }
}
