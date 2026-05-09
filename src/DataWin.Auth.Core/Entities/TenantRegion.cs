using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Entities;

public sealed class TenantRegion
{
    public UuidV7 Id { get; init; }
    public UuidV7 TenantId { get; init; }
    public required RegionCode RegionCode { get; init; }
    public bool IsPrimary { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}
