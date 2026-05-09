using DataWin.Auth.Core.Enums;
using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Entities;

public sealed class UserMfa
{
    public UuidV7 Id { get; init; }
    public UuidV7 UserId { get; init; }
    public UuidV7 TenantId { get; init; }
    public MfaMethod Method { get; init; }
    public required EncryptedField Secret { get; set; }
    public bool IsVerified { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}
