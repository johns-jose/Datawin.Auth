namespace DataWin.Auth.Contracts.Models;

public sealed record TenantContext
{
    public required Guid TenantId { get; init; }
    public required string Slug { get; init; }
    public required string PrimaryRegion { get; init; }
    public required IReadOnlyList<string> Regions { get; init; }
}
