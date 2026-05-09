using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.DTOs;

namespace DataWin.Auth.Application.Commands.Mfa;

public sealed record VerifyMfaCommand : ICommand<AuthResultDto>
{
    public required Guid TenantId { get; init; }
    public required Guid UserId { get; init; }
    public required string Code { get; init; }
    public required string ChallengeToken { get; init; }
    public required string IpAddress { get; init; }
    public required string DeviceFingerprint { get; init; }
}
