using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.DTOs;

namespace DataWin.Auth.Application.Commands.Auth;

public sealed record RefreshTokenCommand : ICommand<TokenResponseDto>
{
    public required Guid TenantId { get; init; }
    public required string RefreshToken { get; init; }
    public required string IpAddress { get; init; }
}
