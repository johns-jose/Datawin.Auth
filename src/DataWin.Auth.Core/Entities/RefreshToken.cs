using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Entities;

public sealed class RefreshToken
{
    public UuidV7 Id { get; init; }
    public UuidV7 SessionId { get; init; }
    public UuidV7 UserId { get; init; }
    public UuidV7 TenantId { get; init; }
    public required string TokenHash { get; init; }
    public bool IsRevoked { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public UuidV7? ReplacedByTokenId { get; set; }
}
