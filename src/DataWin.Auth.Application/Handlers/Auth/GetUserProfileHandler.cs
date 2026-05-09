using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.DTOs;
using DataWin.Auth.Application.Queries;
using DataWin.Auth.Core.Interfaces.Repositories;
using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DataWin.Auth.Application.Handlers.Auth;

public sealed class GetUserProfileHandler : IQueryHandler<GetUserProfileQuery, UserProfileDto?>
{
    private readonly IUserRepository _userRepo;
    private readonly IPiiEncryptionService _encryption;
    private readonly ILogger<GetUserProfileHandler> _logger;

    public GetUserProfileHandler(IUserRepository userRepo, IPiiEncryptionService encryption, ILogger<GetUserProfileHandler> logger)
    {
        _userRepo = userRepo;
        _encryption = encryption;
        _logger = logger;
    }

    public async Task<UserProfileDto?> HandleAsync(GetUserProfileQuery query, CancellationToken ct = default)
    {
        var tenantId = UuidV7.From(query.TenantId);
        var userId = UuidV7.From(query.UserId);

        _logger.LogDebug("Profile requested for user {UserId} in tenant {TenantId}", userId, tenantId);

        var user = await _userRepo.GetByIdAsync(tenantId, userId, ct);
        if (user is null)
        {
            _logger.LogWarning("Profile not found for user {UserId} in tenant {TenantId}", userId, tenantId);
            return null;
        }

        _logger.LogInformation("Profile retrieved for user {UserId} in tenant {TenantId} (PII decrypted)", userId, tenantId);

        return new UserProfileDto
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            Email = _encryption.Decrypt(user.Email, tenantId),
            DisplayName = user.DisplayName is not null ? _encryption.Decrypt(user.DisplayName, tenantId) : null,
            PhoneNumber = user.PhoneNumber is not null ? _encryption.Decrypt(user.PhoneNumber, tenantId) : null,
            MfaEnabled = user.MfaEnabled,
            EmailConfirmed = user.EmailConfirmed,
            CreatedAt = user.CreatedAt
        };
    }
}