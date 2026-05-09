using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Entities;

public sealed class Session
{
    public UuidV7 Id { get; init; }
    public UuidV7 UserId { get; init; }
    public UuidV7 TenantId { get; init; }
    public required string DeviceFingerprint { get; init; }
    public required string IpAddress { get; init; }
    public string? UserAgent { get; set; }
    public bool IsRevoked { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; set; }
}
