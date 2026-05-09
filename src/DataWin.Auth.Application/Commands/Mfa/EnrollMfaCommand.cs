using DataWin.Auth.Application.Abstractions;

namespace DataWin.Auth.Application.Commands.Mfa;

public sealed record EnrollMfaCommand : ICommand<string>
{
    public required Guid TenantId { get; init; }
    public required Guid UserId { get; init; }
    public required string Method { get; init; }
}
