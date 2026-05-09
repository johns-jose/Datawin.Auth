using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.Commands.Auth;
using DataWin.Auth.Application.DTOs;
using DataWin.Auth.Core.Entities;
using DataWin.Auth.Core.Interfaces.Repositories;
using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DataWin.Auth.Application.Handlers.Auth;

public sealed class RegisterUserHandler : ICommandHandler<RegisterUserCommand, RegisterUserResultDto>
{
    private readonly IUserRepository _userRepo;
    private readonly IPiiEncryptionService _encryption;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<RegisterUserHandler> _logger;

    public RegisterUserHandler(
        IUserRepository userRepo,
        IPiiEncryptionService encryption,
        IPasswordHasher passwordHasher,
        ILogger<RegisterUserHandler> logger)
    {
        _userRepo = userRepo;
        _encryption = encryption;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<RegisterUserResultDto> HandleAsync(RegisterUserCommand command, CancellationToken ct = default)
    {
        var tenantId = UuidV7.From(command.TenantId);
        var normalizedEmail = command.Email.ToLowerInvariant();
        var emailHash = _encryption.HashForLookup(normalizedEmail);

        _logger.LogDebug("Registration attempt for tenant {TenantId}", tenantId);

        // Check if user already exists
        var existingUser = await _userRepo.GetByEmailHashAsync(tenantId, emailHash, ct);
        if (existingUser is not null)
        {
            _logger.LogWarning("Registration failed — email already exists for tenant {TenantId}", tenantId);
            return RegisterUserResultDto.Failure("email_exists", "A user with this email already exists.");
        }

        var userId = UuidV7.New();
        var now = DateTimeOffset.UtcNow;

        // Encrypt PII fields
        var encryptedEmail = _encryption.Encrypt(normalizedEmail, tenantId);
        var encryptedDisplayName = command.DisplayName is not null
            ? _encryption.Encrypt(command.DisplayName, tenantId)
            : null;

        var user = new User
        {
            Id = userId,
            TenantId = tenantId,
            Email = encryptedEmail,
            EmailHash = emailHash,
            DisplayName = encryptedDisplayName,
            IsActive = true,
            EmailConfirmed = false,
            MfaEnabled = false,
            FailedLoginAttempts = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _userRepo.CreateAsync(user, ct);

        // Hash password and create credential
        var hashedPassword = _passwordHasher.Hash(command.Password);
        var credential = new UserCredential
        {
            Id = UuidV7.New(),
            UserId = userId,
            TenantId = tenantId,
            PasswordHash = hashedPassword,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _userRepo.CreateCredentialAsync(credential, ct);

        _logger.LogInformation("User {UserId} registered in tenant {TenantId}", userId, tenantId);

        return RegisterUserResultDto.Success((Guid)userId);
    }
}
