using DataWin.Auth.Core.Enums;
using DataWin.Auth.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace DataWin.Auth.Infrastructure.Identity.External;

public sealed class OktaProvider : IAuthenticationScheme
{
    private readonly ILogger<OktaProvider> _logger;

    public OktaProvider(ILogger<OktaProvider> logger) => _logger = logger;
    public AuthSchemeType SchemeType => AuthSchemeType.ExternalOkta;

    public async Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Okta OAuth for tenant {Tenant}", request.TenantSlug);
        await Task.CompletedTask;
        return AuthenticationResult.Failure("Okta provider not yet configured.");
    }
}
