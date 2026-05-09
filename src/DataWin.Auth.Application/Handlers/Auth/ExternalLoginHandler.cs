using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.Commands.Auth;
using DataWin.Auth.Application.DTOs;
using DataWin.Auth.Core.Entities;
using DataWin.Auth.Core.Enums;
using DataWin.Auth.Core.Interfaces.Repositories;
using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DataWin.Auth.Application.Handlers.Auth;

public sealed class ExternalLoginHandler : ICommandHandler<ExternalLoginCommand, AuthResultDto>
{
    private readonly IEnumerable<IAuthenticationScheme> _schemes;
    private readonly IUserRepository _userRepo;
    private readonly ISessionRepository _sessionRepo;
    private readonly ITokenService _tokenService;
    private readonly IPiiEncryptionService _encryption;
    private readonly IRegionalRouter _router;
    private readonly ILogger<ExternalLoginHandler> _logger;

    private const int AccessTokenLifetimeSeconds = 900;

    public ExternalLoginHandler(
        IEnumerable<IAuthenticationScheme> schemes,
        IUserRepository userRepo,
        ISessionRepository sessionRepo,
        ITokenService tokenService,
        IPiiEncryptionService encryption,
        IRegionalRouter router,
        ILogger<ExternalLoginHandler> logger)
    {
        _schemes = schemes;
        _userRepo = userRepo;
        _sessionRepo = sessionRepo;
        _tokenService = tokenService;
        _encryption = encryption;
        _router = router;
        _logger = logger;
    }

    public async Task<AuthResultDto> HandleAsync(ExternalLoginCommand command, CancellationToken ct = default)
    {
        var tenantId = UuidV7.From(command.TenantId);

        _logger.LogDebug("External login attempt via {Provider} for tenant {TenantId} from IP {IpAddress}", command.Provider, tenantId, command.IpAddress);

        if (!Enum.TryParse<AuthSchemeType>(command.Provider, true, out var schemeType))
        {
            _logger.LogWarning("External login failed — unknown provider {Provider} for tenant {TenantId}", command.Provider, tenantId);
            return AuthResultDto.Failure("invalid_provider", $"Unknown provider: {command.Provider}");
        }

        var scheme = _schemes.FirstOrDefault(s => s.SchemeType == schemeType);
        if (scheme is null)
        {
            _logger.LogWarning("External login failed — provider {Provider} not configured for tenant {TenantId}", command.Provider, tenantId);
            return AuthResultDto.Failure("unsupported_provider", $"Provider {command.Provider} is not configured.");
        }

        var authResult = await scheme.AuthenticateAsync(new AuthenticationRequest
        {
            TenantSlug = command.TenantId.ToString(),
            Code = command.Code,
            RedirectUri = command.RedirectUri,
            State = command.State
        }, ct);

        if (!authResult.IsSuccess)
        {
            _logger.LogWarning("External login failed — {Provider} authentication error for tenant {TenantId}: {Error}", command.Provider, tenantId, authResult.ErrorMessage);
            return AuthResultDto.Failure("external_auth_failed", authResult.ErrorMessage ?? "External authentication failed.");
        }

        // Find or create user
        var emailHash = _encryption.HashForLookup(authResult.Email!.ToLowerInvariant());
        var user = await _userRepo.GetByEmailHashAsync(tenantId, emailHash, ct);

        if (user is null)
        {
            user = new User
            {
                Id = UuidV7.New(),
                TenantId = tenantId,
                Email = _encryption.Encrypt(authResult.Email!, tenantId),
                EmailHash = emailHash,
                DisplayName = authResult.DisplayName != null ? _encryption.Encrypt(authResult.DisplayName, tenantId) : null,
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await _userRepo.CreateAsync(user, ct);
        }

        // Link external login
        await _userRepo.LinkExternalLoginAsync(new UserExternalLogin
        {
            Id = UuidV7.New(),
            UserId = user.Id,
            TenantId = tenantId,
            Provider = schemeType,
            ProviderKey = authResult.ExternalUserId!,
            ProviderDisplayName = authResult.DisplayName,
            CreatedAt = DateTimeOffset.UtcNow
        }, ct);

        var region = await _router.ResolvePrimaryRegionAsync(tenantId, ct);
        var session = new Session
        {
            Id = UuidV7.New(),
            UserId = user.Id,
            TenantId = tenantId,
            DeviceFingerprint = command.DeviceFingerprint,
            IpAddress = command.IpAddress,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };
        await _sessionRepo.CreateAsync(session, ct);

        var accessToken = _tokenService.GenerateAccessToken(user.Id, tenantId, region, [], false, "datawin");
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        await _sessionRepo.CreateRefreshTokenAsync(new RefreshToken
        {
            Id = UuidV7.New(),
            SessionId = session.Id,
            UserId = user.Id,
            TenantId = tenantId,
            TokenHash = _encryption.HashForLookup(refreshTokenValue),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        }, ct);

        _logger.LogInformation("External login successful via {Provider} for user {UserId} in tenant {TenantId}, session {SessionId}",
            command.Provider, user.Id, tenantId, session.Id);

        return AuthResultDto.Success(accessToken, refreshTokenValue, AccessTokenLifetimeSeconds);
    }
}
