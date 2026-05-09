using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.Commands.Mfa;
using DataWin.Auth.Application.DTOs;
using DataWin.Auth.Core.Entities;
using DataWin.Auth.Core.Enums;
using DataWin.Auth.Core.Interfaces.Repositories;
using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DataWin.Auth.Application.Handlers.Mfa;

public sealed class VerifyMfaHandler : ICommandHandler<VerifyMfaCommand, AuthResultDto>
{
    private readonly IEnumerable<IMfaProvider> _providers;
    private readonly IUserRepository _userRepo;
    private readonly ISessionRepository _sessionRepo;
    private readonly ITokenService _tokenService;
    private readonly IPiiEncryptionService _encryption;
    private readonly IRegionalRouter _router;
    private readonly ILogger<VerifyMfaHandler> _logger;

    private const int AccessTokenLifetimeSeconds = 900;

    public VerifyMfaHandler(
        IEnumerable<IMfaProvider> providers,
        IUserRepository userRepo,
        ISessionRepository sessionRepo,
        ITokenService tokenService,
        IPiiEncryptionService encryption,
        IRegionalRouter router,
        ILogger<VerifyMfaHandler> logger)
    {
        _providers = providers;
        _userRepo = userRepo;
        _sessionRepo = sessionRepo;
        _tokenService = tokenService;
        _encryption = encryption;
        _router = router;
        _logger = logger;
    }

    public async Task<AuthResultDto> HandleAsync(VerifyMfaCommand command, CancellationToken ct = default)
    {
        var tenantId = UuidV7.From(command.TenantId);
        var userId = UuidV7.From(command.UserId);

        _logger.LogDebug("MFA verification started for user {UserId} in tenant {TenantId}", userId, tenantId);

        var user = await _userRepo.GetByIdAsync(tenantId, userId, ct)
            ?? throw new InvalidOperationException("User not found.");

        // Try each enrolled MFA provider
        foreach (var provider in _providers)
        {
            if (await provider.VerifyAsync(tenantId, userId, command.Code, ct))
            {
                _logger.LogInformation("MFA verified for user {UserId} in tenant {TenantId} via {Method}", userId, tenantId, provider.Method);

                var region = await _router.ResolvePrimaryRegionAsync(tenantId, ct);
                var session = new Session
                {
                    Id = UuidV7.New(),
                    UserId = userId,
                    TenantId = tenantId,
                    DeviceFingerprint = command.DeviceFingerprint,
                    IpAddress = command.IpAddress,
                    CreatedAt = DateTimeOffset.UtcNow,
                    ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
                };
                await _sessionRepo.CreateAsync(session, ct);

                var accessToken = _tokenService.GenerateAccessToken(userId, tenantId, region, [], true, "datawin");
                var refreshTokenValue = _tokenService.GenerateRefreshToken();
                await _sessionRepo.CreateRefreshTokenAsync(new RefreshToken
                {
                    Id = UuidV7.New(),
                    SessionId = session.Id,
                    UserId = userId,
                    TenantId = tenantId,
                    TokenHash = _encryption.HashForLookup(refreshTokenValue),
                    CreatedAt = DateTimeOffset.UtcNow,
                    ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
                }, ct);

                return AuthResultDto.Success(accessToken, refreshTokenValue, AccessTokenLifetimeSeconds);
            }
        }

        _logger.LogWarning("MFA verification failed for user {UserId} in tenant {TenantId}", userId, tenantId);
        return AuthResultDto.Failure("mfa_failed", "MFA verification failed.");
    }
}