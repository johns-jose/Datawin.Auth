using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.Commands.Auth;
using DataWin.Auth.Application.DTOs;
using DataWin.Auth.Core.Entities;
using DataWin.Auth.Core.Interfaces.Repositories;
using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DataWin.Auth.Application.Handlers.Auth;

public sealed class LoginHandler : ICommandHandler<LoginCommand, AuthResultDto>
{
    private readonly IUserRepository _userRepo;
    private readonly ISessionRepository _sessionRepo;
    private readonly ITokenService _tokenService;
    private readonly IPiiEncryptionService _encryption;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRegionalRouter _router;
    private readonly ILogger<LoginHandler> _logger;

    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;
    private const int AccessTokenLifetimeSeconds = 900;

    public LoginHandler(
        IUserRepository userRepo,
        ISessionRepository sessionRepo,
        ITokenService tokenService,
        IPiiEncryptionService encryption,
        IPasswordHasher passwordHasher,
        IRegionalRouter router,
        ILogger<LoginHandler> logger)
    {
        _userRepo = userRepo;
        _sessionRepo = sessionRepo;
        _tokenService = tokenService;
        _encryption = encryption;
        _passwordHasher = passwordHasher;
        _router = router;
        _logger = logger;
    }

    public async Task<AuthResultDto> HandleAsync(LoginCommand command, CancellationToken ct = default)
    {
        var tenantId = UuidV7.From(command.TenantId);
        var emailHash = _encryption.HashForLookup(command.Email.ToLowerInvariant());

        _logger.LogDebug("Login attempt for tenant {TenantId} from IP {IpAddress}", tenantId, command.IpAddress);

        var user = await _userRepo.GetByEmailHashAsync(tenantId, emailHash, ct);
        if (user is null)
        {
            _logger.LogWarning("Login failed — unknown email hash for tenant {TenantId} from IP {IpAddress}", tenantId, command.IpAddress);
            return AuthResultDto.Failure("invalid_credentials", "Invalid email or password.");
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            _logger.LogWarning("Login denied — user {UserId} in tenant {TenantId} is locked out until {LockoutEnd}", user.Id, tenantId, user.LockoutEnd);
            return AuthResultDto.Failure("account_locked", "Account is temporarily locked.");
        }

        var credential = await _userRepo.GetCredentialAsync(tenantId, user.Id, ct);
        if (credential is null)
            return AuthResultDto.Failure("invalid_credentials", "Invalid email or password.");

        if (!_passwordHasher.Verify(command.Password, credential.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(LockoutMinutes);
                _logger.LogWarning("User {UserId} locked out after {Attempts} failed attempts in tenant {TenantId}", user.Id, user.FailedLoginAttempts, tenantId);
            }
            else
            {
                _logger.LogWarning("Login failed — wrong password for user {UserId} in tenant {TenantId}, attempt {Attempt}/{Max}",
                    user.Id, tenantId, user.FailedLoginAttempts, MaxFailedAttempts);
            }
            await _userRepo.UpdateAsync(user, ct);
            return AuthResultDto.Failure("invalid_credentials", "Invalid email or password.");
        }

        // Reset failed attempts on successful password verification
        if (user.FailedLoginAttempts > 0)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _userRepo.UpdateAsync(user, ct);
        }

        // Check if MFA is required
        if (user.MfaEnabled)
        {
            _logger.LogInformation("Login requires MFA for user {UserId} in tenant {TenantId}", user.Id, tenantId);
            var challengeToken = _tokenService.GenerateRefreshToken();
            return AuthResultDto.MfaRequired(challengeToken);
        }

        var region = await _router.ResolvePrimaryRegionAsync(tenantId, ct);

        // Create session
        var session = new Session
        {
            Id = UuidV7.New(),
            UserId = user.Id,
            TenantId = tenantId,
            DeviceFingerprint = command.DeviceFingerprint,
            IpAddress = command.IpAddress,
            UserAgent = command.UserAgent,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };
        await _sessionRepo.CreateAsync(session, ct);

        var accessToken = _tokenService.GenerateAccessToken(
            user.Id, tenantId, region, [], false, command.Audience ?? "datawin");

        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            Id = UuidV7.New(),
            SessionId = session.Id,
            UserId = user.Id,
            TenantId = tenantId,
            TokenHash = _encryption.HashForLookup(refreshTokenValue),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };
        await _sessionRepo.CreateRefreshTokenAsync(refreshToken, ct);

        _logger.LogInformation("Login successful for user {UserId} in tenant {TenantId}, session {SessionId} from IP {IpAddress}",
            user.Id, tenantId, session.Id, command.IpAddress);

        return AuthResultDto.Success(accessToken, refreshTokenValue, AccessTokenLifetimeSeconds);
    }
}
