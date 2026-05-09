using DataWin.Auth.Core.Enums;
using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Entities;

public sealed class Tenant
{
    public UuidV7 Id { get; init; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public TenantStatus Status { get; set; }
    public string? Domain { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
}
