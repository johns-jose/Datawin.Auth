using DataWin.Auth.Core.Enums;
using DataWin.Auth.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace DataWin.Auth.Infrastructure.Identity.External;

public sealed class GoogleProvider : IAuthenticationScheme
{
    private readonly ILogger<GoogleProvider> _logger;

    public GoogleProvider(ILogger<GoogleProvider> logger) => _logger = logger;
    public AuthSchemeType SchemeType => AuthSchemeType.ExternalGoogle;

    public async Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken ct = default)
    {
        // Exchange code with Google OAuth2 token endpoint
        // Validate ID token, extract email/sub/name
        _logger.LogInformation("Google OAuth for tenant {Tenant}", request.TenantSlug);
        await Task.CompletedTask;
        return AuthenticationResult.Failure("Google provider not yet configured.");
    }
}
