using DataWin.Auth.Core.Entities;
using DataWin.Auth.Core.Enums;
using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Interfaces.Repositories;

public interface IConsentRepository
{
    Task<ConsentRecord?> GetAsync(UuidV7 tenantId, UuidV7 userId, ConsentPurpose purpose, CancellationToken ct = default);
    Task<IReadOnlyList<ConsentRecord>> GetAllForUserAsync(UuidV7 tenantId, UuidV7 userId, CancellationToken ct = default);
    Task GrantAsync(ConsentRecord record, CancellationToken ct = default);
    Task WithdrawAsync(UuidV7 tenantId, UuidV7 userId, ConsentPurpose purpose, CancellationToken ct = default);
    Task<DataErasureRequest?> GetErasureRequestAsync(UuidV7 tenantId, UuidV7 requestId, CancellationToken ct = default);
    Task CreateErasureRequestAsync(DataErasureRequest request, CancellationToken ct = default);
    Task UpdateErasureRequestAsync(DataErasureRequest request, CancellationToken ct = default);
}
