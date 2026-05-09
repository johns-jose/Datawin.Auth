using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Entities;

public sealed class User
{
    public UuidV7 Id { get; init; }
    public UuidV7 TenantId { get; init; }
    public required EncryptedField Email { get; set; }
    public required string EmailHash { get; set; }
    public EncryptedField? DisplayName { get; set; }
    public EncryptedField? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool MfaEnabled { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
}
