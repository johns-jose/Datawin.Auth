namespace DataWin.Auth.Core.ValueObjects;

public readonly record struct RegionCode
{
    public string Value { get; }

    public RegionCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.ToLowerInvariant();
    }

    public override string ToString() => Value;

    public static implicit operator string(RegionCode code) => code.Value;
    public static explicit operator RegionCode(string value) => new(value);
}
