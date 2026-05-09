using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Entities;

public sealed class OAuthClient
{
    public UuidV7 Id { get; init; }
    public UuidV7 TenantId { get; init; }
    public required string ClientId { get; init; }
    public required string ClientSecretHash { get; init; }
    public required string DisplayName { get; set; }
    public required string[] RedirectUris { get; set; }
    public required string[] AllowedScopes { get; set; }
    public required string[] AllowedGrantTypes { get; set; }
    public int AccessTokenLifetimeSeconds { get; set; } = 900;
    public int RefreshTokenLifetimeSeconds { get; set; } = 86400;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
}
