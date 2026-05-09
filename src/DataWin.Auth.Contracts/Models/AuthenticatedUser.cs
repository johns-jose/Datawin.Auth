using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Contracts.Models;

public sealed record AuthenticatedUser
{
    public required Guid UserId { get; init; }
    public required Guid TenantId { get; init; }
    public required string Email { get; init; }
    public string? DisplayName { get; init; }
    public required string Region { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
    public required bool MfaVerified { get; init; }
}
