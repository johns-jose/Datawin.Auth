using DataWin.Auth.Core.Enums;
using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Entities;

public sealed class DataErasureRequest
{
    public UuidV7 Id { get; init; }
    public UuidV7 UserId { get; init; }
    public UuidV7 TenantId { get; init; }
    public ErasureStatus Status { get; set; }
    public required string RequestedByIp { get; init; }
    public string? CompletionNotes { get; set; }
    public DateTimeOffset RequestedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; set; }
}
