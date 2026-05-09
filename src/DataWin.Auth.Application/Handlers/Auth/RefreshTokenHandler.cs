using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.Commands.Auth;
using DataWin.Auth.Application.DTOs;
using DataWin.Auth.Core.Entities;
using DataWin.Auth.Core.Interfaces.Repositories;
using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DataWin.Auth.Application.Handlers.Auth;

public sealed class RefreshTokenHandler : ICommandHandler<RefreshTokenCommand, TokenResponseDto>
{
    private readonly ISessionRepository _sessionRepo;
    private readonly IUserRepository _userRepo;
    private readonly ITokenService _tokenService;
    private readonly IPiiEncryptionService _encryption;
    private readonly IRegionalRouter _router;
    private readonly ILogger<RefreshTokenHandler> _logger;

    private const int AccessTokenLifetimeSeconds = 900;

    public RefreshTokenHandler(
        ISessionRepository sessionRepo,
        IUserRepository userRepo,
        ITokenService tokenService,
        IPiiEncryptionService encryption,
        IRegionalRouter router,
        ILogger<RefreshTokenHandler> logger)
    {
        _sessionRepo = sessionRepo;
        _userRepo = userRepo;
        _tokenService = tokenService;
        _encryption = encryption;
        _router = router;
        _logger = logger;
    }

    public async Task<TokenResponseDto> HandleAsync(RefreshTokenCommand command, CancellationToken ct = default)
    {
        var tenantId = UuidV7.From(command.TenantId);
        var tokenHash = _encryption.HashForLookup(command.RefreshToken);

        _logger.LogDebug("Token refresh requested for tenant {TenantId}", tenantId);

        var existingToken = await _sessionRepo.GetRefreshTokenByHashAsync(tenantId, tokenHash, ct);
        if (existingToken is null || existingToken.IsRevoked || existingToken.ExpiresAt < DateTimeOffset.UtcNow)
        {
            _logger.LogWarning("Invalid or expired refresh token for tenant {TenantId}. Revoked={IsRevoked}, Expired={IsExpired}",
                tenantId, existingToken?.IsRevoked, existingToken is not null && existingToken.ExpiresAt < DateTimeOffset.UtcNow);
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        var session = await _sessionRepo.GetByIdAsync(tenantId, existingToken.SessionId, ct);
        if (session is null || session.IsRevoked)
        {
            _logger.LogWarning("Token refresh denied — session {SessionId} revoked for tenant {TenantId}", existingToken.SessionId, tenantId);
            throw new UnauthorizedAccessException("Session has been revoked.");
        }

        var user = await _userRepo.GetByIdAsync(tenantId, existingToken.UserId, ct);
        if (user is null || !user.IsActive)
        {
            _logger.LogWarning("Token refresh denied — user {UserId} inactive for tenant {TenantId}", existingToken.UserId, tenantId);
            throw new UnauthorizedAccessException("User account is not active.");
        }

        // Rotate: revoke old, issue new
        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();
        var newRefreshToken = new RefreshToken
        {
            Id = UuidV7.New(),
            SessionId = session.Id,
            UserId = user.Id,
            TenantId = tenantId,
            TokenHash = _encryption.HashForLookup(newRefreshTokenValue),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };

        await _sessionRepo.RevokeRefreshTokenAsync(tenantId, existingToken.Id, newRefreshToken.Id, ct);
        await _sessionRepo.CreateRefreshTokenAsync(newRefreshToken, ct);

        var region = await _router.ResolvePrimaryRegionAsync(tenantId, ct);
        var accessToken = _tokenService.GenerateAccessToken(
            user.Id, tenantId, region, [], user.MfaEnabled, "datawin");

        _logger.LogInformation("Token rotated for user {UserId} in tenant {TenantId}, session {SessionId}", user.Id, tenantId, session.Id);

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshTokenValue,
            TokenType = "Bearer",
            ExpiresIn = AccessTokenLifetimeSeconds
        };
    }
}