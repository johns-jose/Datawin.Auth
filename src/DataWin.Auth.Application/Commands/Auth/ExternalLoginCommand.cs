using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.DTOs;

namespace DataWin.Auth.Application.Commands.Auth;

public sealed record ExternalLoginCommand : ICommand<AuthResultDto>
{
    public required Guid TenantId { get; init; }
    public required string Provider { get; init; }
    public required string Code { get; init; }
    public required string RedirectUri { get; init; }
    public string? State { get; init; }
    public required string IpAddress { get; init; }
    public required string DeviceFingerprint { get; init; }
}
