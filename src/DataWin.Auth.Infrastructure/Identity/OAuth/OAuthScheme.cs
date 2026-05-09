using DataWin.Auth.Core.Enums;
using DataWin.Auth.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace DataWin.Auth.Infrastructure.Identity.OAuth;

public sealed class OAuthScheme : IAuthenticationScheme
{
    private readonly ILogger<OAuthScheme> _logger;

    public OAuthScheme(ILogger<OAuthScheme> logger) => _logger = logger;

    public AuthSchemeType SchemeType => AuthSchemeType.OAuth2;

    public async Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return AuthenticationResult.Failure("Authorization code is required.");

        // Exchange authorization code for tokens via tenant-configured OAuth endpoint
        // 1. Load tenant OAuth config (token endpoint, client_id, client_secret)
        // 2. POST to token endpoint with code + redirect_uri + code_verifier (PKCE)
        // 3. Validate ID token signature and claims
        // 4. Extract user identity

        _logger.LogInformation("OAuth2 authentication for tenant {Tenant} with code exchange", request.TenantSlug);

        // Placeholder — real implementation exchanges code with IdP
        await Task.CompletedTask;
        return AuthenticationResult.Failure("OAuth2 code exchange not yet implemented. Configure tenant IdP endpoint.");
    }
}
