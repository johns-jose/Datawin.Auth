using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.DTOs;

namespace DataWin.Auth.Application.Queries;

public sealed record ExportUserPiiQuery : IQuery<UserPiiExportDto>
{
    public required Guid TenantId { get; init; }
    public required Guid UserId { get; init; }
}

public sealed record UserPiiExportDto
{
    public required UserProfileDto Profile { get; init; }
    public required ConsentStatusDto Consents { get; init; }
    public required IReadOnlyList<ExternalLoginDto> ExternalLogins { get; init; }
    public DateTimeOffset ExportedAt { get; init; }
}

public sealed record ExternalLoginDto
{
    public required string Provider { get; init; }
    public string? DisplayName { get; init; }
    public DateTimeOffset LinkedAt { get; init; }
}
