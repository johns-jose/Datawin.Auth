using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Interfaces;

public interface ITenantContext
{
    TenantId TenantId { get; }
    RegionCode PrimaryRegion { get; }
    bool IsResolved { get; }
}
