using DataWin.Auth.Application.Abstractions;
using DataWin.Auth.Application.Commands.Consent;
using DataWin.Auth.Core.Entities;
using DataWin.Auth.Core.Enums;
using DataWin.Auth.Core.Interfaces.Repositories;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DataWin.Auth.Application.Handlers.Consent;

public sealed class GrantConsentHandler : ICommandHandler<GrantConsentCommand, bool>
{
    private readonly IConsentRepository _consentRepo;
    private readonly ILogger<GrantConsentHandler> _logger;

    public GrantConsentHandler(IConsentRepository consentRepo, ILogger<GrantConsentHandler> logger)
    {
        _consentRepo = consentRepo;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(GrantConsentCommand command, CancellationToken ct = default)
    {
        if (!Enum.TryParse<ConsentPurpose>(command.Purpose, true, out var purpose))
            throw new ArgumentException($"Unknown consent purpose: {command.Purpose}");

        var record = new ConsentRecord
        {
            Id = UuidV7.New(),
            UserId = UuidV7.From(command.UserId),
            TenantId = UuidV7.From(command.TenantId),
            Purpose = purpose,
            IsGranted = true,
            IpAddress = command.IpAddress,
            GrantedAt = DateTimeOffset.UtcNow
        };

        await _consentRepo.GrantAsync(record, ct);

        _logger.LogInformation("Consent granted for user {UserId} in tenant {TenantId}, purpose {Purpose} from IP {IpAddress}",
            command.UserId, command.TenantId, command.Purpose, command.IpAddress);

        return true;
    }
}