using DataWin.Auth.Application.Abstractions;

namespace DataWin.Auth.Application.Commands.Auth;

public sealed record LogoutCommand : ICommand<bool>
{
    public required Guid TenantId { get; init; }
    public required Guid UserId { get; init; }
    public required Guid SessionId { get; init; }
}
