using DataWin.Auth.Core.Enums;
using DataWin.Auth.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace DataWin.Auth.Infrastructure.Identity.External;

public sealed class AzureAdProvider : IAuthenticationScheme
{
    private readonly ILogger<AzureAdProvider> _logger;

    public AzureAdProvider(ILogger<AzureAdProvider> logger) => _logger = logger;
    public AuthSchemeType SchemeType => AuthSchemeType.ExternalAzureAd;

    public async Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Azure AD OAuth for tenant {Tenant}", request.TenantSlug);
        await Task.CompletedTask;
        return AuthenticationResult.Failure("Azure AD provider not yet configured.");
    }
}
