using DataWin.Auth.Core.Entities;
using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(UuidV7 tenantId, UuidV7 userId, CancellationToken ct = default);
    Task<User?> GetByEmailHashAsync(UuidV7 tenantId, string emailHash, CancellationToken ct = default);
    Task CreateAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task<UserCredential?> GetCredentialAsync(UuidV7 tenantId, UuidV7 userId, CancellationToken ct = default);
    Task CreateCredentialAsync(UserCredential credential, CancellationToken ct = default);
    Task UpdateCredentialAsync(UserCredential credential, CancellationToken ct = default);
    Task<IReadOnlyList<UserExternalLogin>> GetExternalLoginsAsync(UuidV7 tenantId, UuidV7 userId, CancellationToken ct = default);
    Task LinkExternalLoginAsync(UserExternalLogin login, CancellationToken ct = default);
}
