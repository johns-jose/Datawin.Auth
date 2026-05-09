using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Interfaces.Services;

public interface IDataErasureService
{
    Task RequestErasureAsync(UuidV7 tenantId, UuidV7 userId, string requestedByIp, CancellationToken ct = default);
    Task ProcessErasureAsync(UuidV7 tenantId, UuidV7 requestId, CancellationToken ct = default);
}
