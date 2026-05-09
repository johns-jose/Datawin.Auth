namespace DataWin.Auth.Application.DTOs;

public sealed record AuthResultDto
{
    public bool IsSuccess { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public int ExpiresIn { get; init; }
    public string? TokenType { get; init; }
    public bool RequiresMfa { get; init; }
    public string? MfaChallengeToken { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static AuthResultDto Success(string accessToken, string refreshToken, int expiresIn)
        => new() { IsSuccess = true, AccessToken = accessToken, RefreshToken = refreshToken, ExpiresIn = expiresIn, TokenType = "Bearer" };

    public static AuthResultDto MfaRequired(string challengeToken)
        => new() { IsSuccess = false, RequiresMfa = true, MfaChallengeToken = challengeToken };

    public static AuthResultDto Failure(string errorCode, string message)
        => new() { IsSuccess = false, ErrorCode = errorCode, ErrorMessage = message };
}
