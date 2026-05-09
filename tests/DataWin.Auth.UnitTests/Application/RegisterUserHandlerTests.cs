using DataWin.Auth.Application.Commands.Auth;
using DataWin.Auth.Application.Handlers.Auth;
using DataWin.Auth.Core.Entities;
using DataWin.Auth.Core.Interfaces.Repositories;
using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DataWin.Auth.UnitTests.Application;

public class RegisterUserHandlerTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IPiiEncryptionService _encryption = Substitute.For<IPiiEncryptionService>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ILogger<RegisterUserHandler> _logger = Substitute.For<ILogger<RegisterUserHandler>>();

    private RegisterUserHandler CreateHandler() => new(
        _userRepo, _encryption, _passwordHasher, _logger);

    [Fact]
    public async Task HandleAsync_NewUser_CreatesUserAndCredential()
    {
        _encryption.HashForLookup(Arg.Any<string>()).Returns("email-hash");
        _encryption.Encrypt(Arg.Any<string>(), Arg.Any<UuidV7>())
            .Returns(EncryptedField.FromComponents([1], [2], [3], "k1"));
        _userRepo.GetByEmailHashAsync(Arg.Any<UuidV7>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _passwordHasher.Hash(Arg.Any<string>())
            .Returns(new HashedPassword { Hash = "salt.hash", Algorithm = HashedPassword.Argon2Id });

        var handler = CreateHandler();
        var command = new RegisterUserCommand
        {
            TenantId = Guid.NewGuid(),
            Email = "user@example.com",
            Password = "P@ssw0rd!",
            DisplayName = "Test User"
        };

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.UserId);
        await _userRepo.Received(1).CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _userRepo.Received(1).CreateCredentialAsync(Arg.Any<UserCredential>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DuplicateEmail_ReturnsFailure()
    {
        var existingUser = new User
        {
            Id = UuidV7.New(),
            TenantId = UuidV7.New(),
            Email = EncryptedField.FromComponents([1], [2], [3], "k1"),
            EmailHash = "email-hash",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _encryption.HashForLookup(Arg.Any<string>()).Returns("email-hash");
        _userRepo.GetByEmailHashAsync(Arg.Any<UuidV7>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existingUser);

        var handler = CreateHandler();
        var command = new RegisterUserCommand
        {
            TenantId = Guid.NewGuid(),
            Email = "user@example.com",
            Password = "P@ssw0rd!"
        };

        var result = await handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal("email_exists", result.ErrorCode);
        await _userRepo.DidNotReceive().CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _userRepo.DidNotReceive().CreateCredentialAsync(Arg.Any<UserCredential>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NoDisplayName_CreatesUserWithoutDisplayName()
    {
        _encryption.HashForLookup(Arg.Any<string>()).Returns("email-hash");
        _encryption.Encrypt(Arg.Any<string>(), Arg.Any<UuidV7>())
            .Returns(EncryptedField.FromComponents([1], [2], [3], "k1"));
        _userRepo.GetByEmailHashAsync(Arg.Any<UuidV7>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _passwordHasher.Hash(Arg.Any<string>())
            .Returns(new HashedPassword { Hash = "salt.hash", Algorithm = HashedPassword.Argon2Id });

        var handler = CreateHandler();
        var command = new RegisterUserCommand
        {
            TenantId = Guid.NewGuid(),
            Email = "user@example.com",
            Password = "P@ssw0rd!"
        };

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        await _userRepo.Received(1).CreateAsync(
            Arg.Is<User>(u => u.DisplayName == null), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CreatesCredentialWithHashedPassword()
    {
        var expectedHash = new HashedPassword { Hash = "c2FsdA==.aGFzaA==", Algorithm = HashedPassword.Argon2Id };

        _encryption.HashForLookup(Arg.Any<string>()).Returns("email-hash");
        _encryption.Encrypt(Arg.Any<string>(), Arg.Any<UuidV7>())
            .Returns(EncryptedField.FromComponents([1], [2], [3], "k1"));
        _userRepo.GetByEmailHashAsync(Arg.Any<UuidV7>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _passwordHasher.Hash("P@ssw0rd!").Returns(expectedHash);

        var handler = CreateHandler();
        var command = new RegisterUserCommand
        {
            TenantId = Guid.NewGuid(),
            Email = "user@example.com",
            Password = "P@ssw0rd!"
        };

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        await _userRepo.Received(1).CreateCredentialAsync(
            Arg.Is<UserCredential>(c => c.PasswordHash == expectedHash), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NormalizesEmailToLowercase()
    {
        _encryption.HashForLookup(Arg.Any<string>()).Returns("email-hash");
        _encryption.Encrypt(Arg.Any<string>(), Arg.Any<UuidV7>())
            .Returns(EncryptedField.FromComponents([1], [2], [3], "k1"));
        _userRepo.GetByEmailHashAsync(Arg.Any<UuidV7>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _passwordHasher.Hash(Arg.Any<string>())
            .Returns(new HashedPassword { Hash = "salt.hash", Algorithm = HashedPassword.Argon2Id });

        var handler = CreateHandler();
        var command = new RegisterUserCommand
        {
            TenantId = Guid.NewGuid(),
            Email = "User@EXAMPLE.COM",
            Password = "P@ssw0rd!"
        };

        await handler.HandleAsync(command);

        _encryption.Received(1).HashForLookup("user@example.com");
        _encryption.Received(1).Encrypt("user@example.com", Arg.Any<UuidV7>());
    }
}
