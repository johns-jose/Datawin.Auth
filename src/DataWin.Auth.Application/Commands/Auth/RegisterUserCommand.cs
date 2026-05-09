using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.DTOs;

namespace DataWin.Auth.Application.Commands.Auth;

public sealed record RegisterUserCommand : ICommand<RegisterUserResultDto>
{
    public required Guid TenantId { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public string? DisplayName { get; init; }
}
