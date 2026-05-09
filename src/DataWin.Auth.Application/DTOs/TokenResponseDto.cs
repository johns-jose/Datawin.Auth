namespace DataWin.Auth.Application.DTOs;

public sealed record TokenResponseDto
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required string TokenType { get; init; }
    public required int ExpiresIn { get; init; }
}
