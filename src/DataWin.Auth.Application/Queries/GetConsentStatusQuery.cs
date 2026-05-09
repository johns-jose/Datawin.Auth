using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.DTOs;

namespace DataWin.Auth.Application.Queries;

public sealed record GetConsentStatusQuery : IQuery<ConsentStatusDto>
{
    public required Guid TenantId { get; init; }
    public required Guid UserId { get; init; }
}
