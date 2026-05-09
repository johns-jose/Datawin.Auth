namespace DataWin.Auth.Core.ValueObjects;

public readonly record struct TenantId
{
    public UuidV7 Value { get; }

    public TenantId(UuidV7 value) => Value = value;
    public static TenantId New() => new(UuidV7.New());
    public static TenantId From(Guid guid) => new(UuidV7.From(guid));
    public static TenantId Empty => new(UuidV7.Empty);

    public bool IsEmpty => Value.IsEmpty;
    public override string ToString() => Value.ToString();

    public static implicit operator Guid(TenantId id) => id.Value;
}
