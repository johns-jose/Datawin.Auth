using DataWin.Auth.Application.Commands.Auth;
using DataWin.Auth.Application.DTOs;
using DataWin.Auth.Application.Handlers.Auth;
using DataWin.Auth.Core.Entities;
using DataWin.Auth.Core.Interfaces.Repositories;
using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DataWin.Auth.UnitTests.Application;

public class LoginHandlerTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly ISessionRepository _sessionRepo = Substitute.For<ISessionRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IPiiEncryptionService _encryption = Substitute.For<IPiiEncryptionService>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IRegionalRouter _router = Substitute.For<IRegionalRouter>();
    private readonly ILogger<LoginHandler> _logger = Substitute.For<ILogger<LoginHandler>>();

    private LoginHandler CreateHandler() => new(
        _userRepo, _sessionRepo, _tokenService, _encryption, _passwordHasher, _router, _logger);

    [Fact]
    public async Task HandleAsync_InvalidCredentials_ReturnsFailure()
    {
        _encryption.HashForLookup(Arg.Any<string>()).Returns("hash");
        _userRepo.GetByEmailHashAsync(Arg.Any<UuidV7>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var handler = CreateHandler();
        var command = new LoginCommand
        {
            TenantId = Guid.NewGuid(),
            Email = "test@test.com",
            Password = "password123",
            IpAddress = "127.0.0.1",
            DeviceFingerprint = "fp-123"
        };

        var result = await handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal("invalid_credentials", result.ErrorCode);
    }

    [Fact]
    public async Task HandleAsync_LockedAccount_ReturnsFailure()
    {
        var user = new User
        {
            Id = UuidV7.New(),
            TenantId = UuidV7.New(),
            Email = EncryptedField.FromComponents([], [], [], "k"),
            EmailHash = "hash",
            LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(10),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _encryption.HashForLookup(Arg.Any<string>()).Returns("hash");
        _userRepo.GetByEmailHashAsync(Arg.Any<UuidV7>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var handler = CreateHandler();
        var command = new LoginCommand
        {
            TenantId = Guid.NewGuid(),
            Email = "test@test.com",
            Password = "password123",
            IpAddress = "127.0.0.1",
            DeviceFingerprint = "fp-123"
        };

        var result = await handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal("account_locked", result.ErrorCode);
    }

    [Fact]
    public async Task HandleAsync_WrongPassword_IncrementsFailedAttempts()
    {
        var user = new User
        {
            Id = UuidV7.New(),
            TenantId = UuidV7.New(),
            Email = EncryptedField.FromComponents([], [], [], "k"),
            EmailHash = "hash",
            FailedLoginAttempts = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var credential = new UserCredential
        {
            Id = UuidV7.New(),
            UserId = user.Id,
            TenantId = user.TenantId,
            PasswordHash = new HashedPassword { Hash = "stored", Algorithm = "argon2id" },
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _encryption.HashForLookup(Arg.Any<string>()).Returns("hash");
        _userRepo.GetByEmailHashAsync(Arg.Any<UuidV7>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _userRepo.GetCredentialAsync(Arg.Any<UuidV7>(), Arg.Any<UuidV7>(), Arg.Any<CancellationToken>()).Returns(credential);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<HashedPassword>()).Returns(false);

        var handler = CreateHandler();
        var command = new LoginCommand
        {
            TenantId = Guid.NewGuid(),
            Email = "test@test.com",
            Password = "wrong",
            IpAddress = "127.0.0.1",
            DeviceFingerprint = "fp-123"
        };

        var result = await handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(1, user.FailedLoginAttempts);
        await _userRepo.Received(1).UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SuccessfulLogin_ReturnsTokens()
    {
        var tenantId = UuidV7.New();
        var user = new User
        {
            Id = UuidV7.New(),
            TenantId = tenantId,
            Email = EncryptedField.FromComponents([], [], [], "k"),
            EmailHash = "hash",
            IsActive = true,
            MfaEnabled = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var credential = new UserCredential
        {
            Id = UuidV7.New(),
            UserId = user.Id,
            TenantId = tenantId,
            PasswordHash = new HashedPassword { Hash = "stored", Algorithm = "argon2id" },
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _encryption.HashForLookup(Arg.Any<string>()).Returns("hash");
        _userRepo.GetByEmailHashAsync(Arg.Any<UuidV7>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _userRepo.GetCredentialAsync(Arg.Any<UuidV7>(), Arg.Any<UuidV7>(), Arg.Any<CancellationToken>()).Returns(credential);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<HashedPassword>()).Returns(true);
        _router.ResolvePrimaryRegionAsync(Arg.Any<UuidV7>(), Arg.Any<CancellationToken>()).Returns(new RegionCode("us-east-1"));
        _tokenService.GenerateAccessToken(Arg.Any<UuidV7>(), Arg.Any<UuidV7>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>(), Arg.Any<string>()).Returns("access-token");
        _tokenService.GenerateRefreshToken().Returns("refresh-token");

        var handler = CreateHandler();
        var command = new LoginCommand
        {
            TenantId = (Guid)tenantId,
            Email = "test@test.com",
            Password = "password123",
            IpAddress = "127.0.0.1",
            DeviceFingerprint = "fp-123"
        };

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.Equal("Bearer", result.TokenType);
        await _sessionRepo.Received(1).CreateAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>());
        await _sessionRepo.Received(1).CreateRefreshTokenAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MfaEnabled_ReturnsMfaRequired()
    {
        var tenantId = UuidV7.New();
        var user = new User
        {
            Id = UuidV7.New(),
            TenantId = tenantId,
            Email = EncryptedField.FromComponents([], [], [], "k"),
            EmailHash = "hash",
            MfaEnabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var credential = new UserCredential
        {
            Id = UuidV7.New(),
            UserId = user.Id,
            TenantId = tenantId,
            PasswordHash = new HashedPassword { Hash = "stored", Algorithm = "argon2id" },
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _encryption.HashForLookup(Arg.Any<string>()).Returns("hash");
        _userRepo.GetByEmailHashAsync(Arg.Any<UuidV7>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _userRepo.GetCredentialAsync(Arg.Any<UuidV7>(), Arg.Any<UuidV7>(), Arg.Any<CancellationToken>()).Returns(credential);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<HashedPassword>()).Returns(true);
        _tokenService.GenerateRefreshToken().Returns("mfa-challenge");

        var handler = CreateHandler();
        var result = await handler.HandleAsync(new LoginCommand
        {
            TenantId = (Guid)tenantId,
            Email = "test@test.com",
            Password = "password123",
            IpAddress = "127.0.0.1",
            DeviceFingerprint = "fp-123"
        });

        Assert.False(result.IsSuccess);
        Assert.True(result.RequiresMfa);
        Assert.Equal("mfa-challenge", result.MfaChallengeToken);
    }
}
