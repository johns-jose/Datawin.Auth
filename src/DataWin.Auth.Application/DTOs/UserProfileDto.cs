namespace DataWin.Auth.Application.DTOs;

public sealed record UserProfileDto
{
    public required Guid UserId { get; init; }
    public required Guid TenantId { get; init; }
    public required string Email { get; init; }
    public string? DisplayName { get; init; }
    public string? PhoneNumber { get; init; }
    public bool MfaEnabled { get; init; }
    public bool EmailConfirmed { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
