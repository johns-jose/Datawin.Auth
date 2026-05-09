using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.DTOs;
using DataWin.Auth.Application.Queries;
using DataWin.Auth.Core.Interfaces.Repositories;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DataWin.Auth.Application.Handlers.Consent;

public sealed class GetConsentStatusHandler : IQueryHandler<GetConsentStatusQuery, ConsentStatusDto>
{
    private readonly IConsentRepository _consentRepo;
    private readonly ILogger<GetConsentStatusHandler> _logger;

    public GetConsentStatusHandler(IConsentRepository consentRepo, ILogger<GetConsentStatusHandler> logger)
    {
        _consentRepo = consentRepo;
        _logger = logger;
    }

    public async Task<ConsentStatusDto> HandleAsync(GetConsentStatusQuery query, CancellationToken ct = default)
    {
        _logger.LogDebug("Consent status requested for user {UserId} in tenant {TenantId}", query.UserId, query.TenantId);

        var records = await _consentRepo.GetAllForUserAsync(
            UuidV7.From(query.TenantId), UuidV7.From(query.UserId), ct);

        _logger.LogInformation("Consent status retrieved for user {UserId} in tenant {TenantId}, {Count} records",
            query.UserId, query.TenantId, records.Count);

        return new ConsentStatusDto
        {
            UserId = query.UserId,
            Consents = records.Select(r => new ConsentEntryDto
            {
                Purpose = r.Purpose.ToString(),
                IsGranted = r.IsGranted,
                GrantedAt = r.GrantedAt,
                WithdrawnAt = r.WithdrawnAt
            }).ToList()
        };
    }
}