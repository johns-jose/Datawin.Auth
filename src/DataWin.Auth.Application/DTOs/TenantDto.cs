namespace DataWin.Auth.Application.DTOs;

public sealed record TenantDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? Domain { get; init; }
    public required string Status { get; init; }
    public required IReadOnlyList<TenantRegionDto> Regions { get; init; }
}

public sealed record TenantRegionDto
{
    public required string RegionCode { get; init; }
    public required bool IsPrimary { get; init; }
}
