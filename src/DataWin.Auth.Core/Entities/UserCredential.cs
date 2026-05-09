using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Entities;

public sealed class UserCredential
{
    public UuidV7 Id { get; init; }
    public UuidV7 UserId { get; init; }
    public UuidV7 TenantId { get; init; }
    public required HashedPassword PasswordHash { get; set; }
    public string? RecoveryCodes { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
}
