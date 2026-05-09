using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Entities;

public sealed class PiiAuditEntry
{
    public UuidV7 Id { get; init; }
    public UuidV7 TenantId { get; init; }
    public UuidV7 UserId { get; init; }
    public UuidV7? ActorId { get; init; }
    public required string Action { get; init; }
    public required string FieldName { get; init; }
    public required string Reason { get; init; }
    public required string IpAddress { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}
