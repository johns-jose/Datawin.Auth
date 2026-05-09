using DataWin.Auth.Core.Entities;
using DataWin.Auth.Core.Interfaces.Repositories;
using DataWin.Auth.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DataWin.Auth.Infrastructure.Privacy;

public sealed class PiiAuditLogger
{
    private readonly IPiiAuditRepository _auditRepo;
    private readonly ILogger<PiiAuditLogger> _logger;

    public PiiAuditLogger(IPiiAuditRepository auditRepo, ILogger<PiiAuditLogger> logger)
    {
        _auditRepo = auditRepo;
        _logger = logger;
    }

    public async Task LogAccessAsync(UuidV7 tenantId, UuidV7 userId, UuidV7? actorId, string fieldName, string reason, string ipAddress, CancellationToken ct = default)
    {
        var entry = new PiiAuditEntry
        {
            Id = UuidV7.New(),
            TenantId = tenantId,
            UserId = userId,
            ActorId = actorId,
            Action = "ACCESS",
            FieldName = fieldName,
            Reason = reason,
            IpAddress = ipAddress,
            Timestamp = DateTimeOffset.UtcNow
        };

        await _auditRepo.WriteAsync(entry, ct);
        _logger.LogInformation("PII access logged: {Field} for user {UserId} by {Actor}", fieldName, userId, actorId);
    }

    public async Task LogMutationAsync(UuidV7 tenantId, UuidV7 userId, UuidV7? actorId, string fieldName, string reason, string ipAddress, CancellationToken ct = default)
    {
        var entry = new PiiAuditEntry
        {
            Id = UuidV7.New(),
            TenantId = tenantId,
            UserId = userId,
            ActorId = actorId,
            Action = "MUTATION",
            FieldName = fieldName,
            Reason = reason,
            IpAddress = ipAddress,
            Timestamp = DateTimeOffset.UtcNow
        };

        await _auditRepo.WriteAsync(entry, ct);
    }
}
