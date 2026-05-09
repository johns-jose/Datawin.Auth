using DataWin.Auth.Core.Interfaces;
using DataWin.Auth.Core.Interfaces.Repositories;
using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Infrastructure.Database;
using DataWin.Auth.Infrastructure.Database.Repositories;
using DataWin.Auth.Infrastructure.Encryption;
using DataWin.Auth.Infrastructure.Identity.External;
using DataWin.Auth.Infrastructure.Identity.Mfa;
using DataWin.Auth.Infrastructure.Identity.OAuth;
using DataWin.Auth.Infrastructure.Identity.OpenIdConnect;
using DataWin.Auth.Infrastructure.Identity.Saml;
using DataWin.Auth.Infrastructure.Privacy;
using DataWin.Auth.Infrastructure.Tokens;
using Microsoft.Extensions.DependencyInjection;

namespace DataWin.Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDataWinAuthInfrastructure(
        this IServiceCollection services,
        string globalConnectionString,
        JwtTokenSettings jwtSettings)
    {
        // Database
        services.AddMemoryCache();
        services.AddSingleton<IRegionalRouter>(sp =>
            new RegionalDbRouter(globalConnectionString, sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>()));
        services.AddSingleton<IDbConnectionFactory>(sp =>
            new PostgresConnectionFactory(
                globalConnectionString,
                sp.GetRequiredService<IRegionalRouter>(),
                sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<PostgresConnectionFactory>>()));

        // Repositories
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IConsentRepository, ConsentRepository>();
        services.AddScoped<IOAuthClientRepository, OAuthClientRepository>();
        services.AddScoped<IPiiAuditRepository, PiiAuditRepository>();

        // Tokens
        services.AddSingleton<ITokenService>(new JwtTokenService(jwtSettings));

        // Encryption
        services.AddSingleton<IKeyManagementService, KeyManagementService>();
        services.AddSingleton<IPiiEncryptionService, AesGcmFieldEncryptor>();
        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();

        // Data erasure
        services.AddScoped<IDataErasureService, DataErasureService>();
        services.AddScoped<PiiAuditLogger>();

        // Authentication schemes
        services.AddSingleton<IAuthenticationScheme, OAuthScheme>();
        services.AddSingleton<IAuthenticationScheme, OidcScheme>();
        services.AddSingleton<IAuthenticationScheme, SamlScheme>();
        services.AddSingleton<IAuthenticationScheme, GoogleProvider>();
        services.AddSingleton<IAuthenticationScheme, AzureAdProvider>();
        services.AddSingleton<IAuthenticationScheme, OktaProvider>();

        // MFA providers
        services.AddSingleton<IMfaProvider, TotpProvider>();
        services.AddSingleton<IMfaProvider, EmailMfaProvider>();
        services.AddSingleton<IMfaProvider, SmsMfaProvider>();
        services.AddSingleton<IMfaProvider, WebAuthnProvider>();

        return services;
    }
}

