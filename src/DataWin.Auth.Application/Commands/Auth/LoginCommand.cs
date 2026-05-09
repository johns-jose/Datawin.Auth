using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.DTOs;

namespace DataWin.Auth.Application.Commands.Auth;

public sealed record LoginCommand : ICommand<AuthResultDto>
{
    public required Guid TenantId { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string IpAddress { get; init; }
    public required string DeviceFingerprint { get; init; }
    public string? UserAgent { get; init; }
    public string? Audience { get; init; }
}
