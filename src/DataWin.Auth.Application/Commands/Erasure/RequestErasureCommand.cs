using DataWin.Auth.Application.Abstractions;

namespace DataWin.Auth.Application.Commands.Erasure;

public sealed record RequestErasureCommand : ICommand<Guid>
{
    public required Guid TenantId { get; init; }
    public required Guid UserId { get; init; }
    public required string IpAddress { get; init; }
}
