using DataWin.Auth.Core.Entities;
using DataWin.Auth.Core.Enums;
using DataWin.Auth.Core.Interfaces.Repositories;
using DataWin.Auth.Core.Interfaces.Services;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DataWin.Auth.Infrastructure.Privacy;

public sealed class DataErasureService : IDataErasureService
{
    private readonly IConsentRepository _consentRepo;
    private readonly IPiiEncryptionService _encryption;
    private readonly ILogger<DataErasureService> _logger;

    public DataErasureService(
        IConsentRepository consentRepo,
        IPiiEncryptionService encryption,
        ILogger<DataErasureService> logger)
    {
        _consentRepo = consentRepo;
        _encryption = encryption;
        _logger = logger;
    }

    public async Task RequestErasureAsync(UuidV7 tenantId, UuidV7 userId, string requestedByIp, CancellationToken ct = default)
    {
        var request = new DataErasureRequest
        {
            Id = UuidV7.New(),
            UserId = userId,
            TenantId = tenantId,
            Status = ErasureStatus.Requested,
            RequestedByIp = requestedByIp,
            RequestedAt = DateTimeOffset.UtcNow
        };

        await _consentRepo.CreateErasureRequestAsync(request, ct);
        _logger.LogInformation("Erasure requested for user {UserId} in tenant {TenantId}", userId, tenantId);
    }

    public async Task ProcessErasureAsync(UuidV7 tenantId, UuidV7 requestId, CancellationToken ct = default)
    {
        var request = await _consentRepo.GetErasureRequestAsync(tenantId, requestId, ct)
            ?? throw new InvalidOperationException($"Erasure request {requestId} not found.");

        request.Status = ErasureStatus.InProgress;
        await _consentRepo.UpdateErasureRequestAsync(request, ct);

        try
        {
            // Crypto-shredding: destroy the tenant/user DEK
            await _encryption.DestroyKeyAsync(tenantId, ct);

            request.Status = ErasureStatus.Completed;
            request.CompletedAt = DateTimeOffset.UtcNow;
            request.CompletionNotes = "Crypto-shredding completed. DEK destroyed.";
        }
        catch (Exception ex)
        {
            request.Status = ErasureStatus.Failed;
            request.CompletionNotes = $"Erasure failed: {ex.Message}";
            _logger.LogError(ex, "Erasure failed for request {RequestId}", requestId);
        }

        await _consentRepo.UpdateErasureRequestAsync(request, ct);
    }
}
