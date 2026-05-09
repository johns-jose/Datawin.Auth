using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Entities;

public sealed class SamlConfiguration
{
    public UuidV7 Id { get; init; }
    public UuidV7 TenantId { get; init; }
    public required string EntityId { get; init; }
    public required string MetadataUrl { get; set; }
    public required string AssertionConsumerServiceUrl { get; set; }
    public required string SingleLogoutServiceUrl { get; set; }
    public required string CertificateBase64 { get; set; }
    public bool SignRequests { get; set; } = true;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
}
