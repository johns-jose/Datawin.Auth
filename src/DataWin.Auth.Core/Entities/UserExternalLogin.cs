using DataWin.Auth.Core.Enums;
using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Entities;

public sealed class UserExternalLogin
{
    public UuidV7 Id { get; init; }
    public UuidV7 UserId { get; init; }
    public UuidV7 TenantId { get; init; }
    public AuthSchemeType Provider { get; init; }
    public required string ProviderKey { get; init; }
    public string? ProviderDisplayName { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}
