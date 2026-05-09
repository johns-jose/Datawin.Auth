namespace DataWin.Auth.Contracts.Models;

public sealed record TokenResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required string TokenType { get; init; }
    public required int ExpiresIn { get; init; }
    public string? IdToken { get; init; }
}
