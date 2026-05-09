namespace DataWin.Auth.Application.DTOs;

public sealed record RegisterUserResultDto
{
    public bool IsSuccess { get; init; }
    public Guid? UserId { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static RegisterUserResultDto Success(Guid userId)
        => new() { IsSuccess = true, UserId = userId };

    public static RegisterUserResultDto Failure(string errorCode, string message)
        => new() { IsSuccess = false, ErrorCode = errorCode, ErrorMessage = message };
}
