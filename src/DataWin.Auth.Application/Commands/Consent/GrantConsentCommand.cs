using DataWin.Auth.Application.Abstractions;

namespace DataWin.Auth.Application.Commands.Consent;

public sealed record GrantConsentCommand : ICommand<bool>
{
    public required Guid TenantId { get; init; }
    public required Guid UserId { get; init; }
    public required string Purpose { get; init; }
    public required string IpAddress { get; init; }
}
