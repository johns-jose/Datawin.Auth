using System.Data;

namespace DataWin.Auth.Core.Interfaces;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateGlobalConnectionAsync(CancellationToken ct = default);
    Task<IDbConnection> CreateRegionalConnectionAsync(string regionCode, CancellationToken ct = default);
}
