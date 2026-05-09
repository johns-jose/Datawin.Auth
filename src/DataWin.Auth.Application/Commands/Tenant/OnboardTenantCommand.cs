using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.DTOs;

namespace DataWin.Auth.Application.Commands.Tenant;

public sealed record OnboardTenantCommand : ICommand<TenantDto>
{
    public required Guid TenantId { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? Domain { get; init; }
    public required string PrimaryRegion { get; init; }
    public IReadOnlyList<string>? AdditionalRegions { get; init; }
}
