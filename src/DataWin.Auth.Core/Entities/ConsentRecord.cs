using DataWin.Auth.Core.Enums;
using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Entities;

public sealed class ConsentRecord
{
    public UuidV7 Id { get; init; }
    public UuidV7 UserId { get; init; }
    public UuidV7 TenantId { get; init; }
    public ConsentPurpose Purpose { get; init; }
    public bool IsGranted { get; set; }
    public required string IpAddress { get; init; }
    public DateTimeOffset GrantedAt { get; init; }
    public DateTimeOffset? WithdrawnAt { get; set; }
}
