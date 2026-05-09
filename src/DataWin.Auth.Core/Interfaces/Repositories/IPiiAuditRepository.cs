using DataWin.Auth.Core.Entities;
using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Interfaces.Repositories;

public interface IPiiAuditRepository
{
    Task WriteAsync(PiiAuditEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<PiiAuditEntry>> GetByUserAsync(UuidV7 tenantId, UuidV7 userId, CancellationToken ct = default);
}
