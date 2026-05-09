using System.Security.Cryptography;

namespace DataWin.Auth.Core.ValueObjects;

public readonly struct UuidV7 : IEquatable<UuidV7>, IComparable<UuidV7>
{
    public Guid Value { get; }

    private UuidV7(Guid value) => Value = value;

    public static UuidV7 New()
    {
        Span<byte> bytes = stackalloc byte[16];
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        bytes[0] = (byte)(timestamp >> 40);
        bytes[1] = (byte)(timestamp >> 32);
        bytes[2] = (byte)(timestamp >> 24);
        bytes[3] = (byte)(timestamp >> 16);
        bytes[4] = (byte)(timestamp >> 8);
        bytes[5] = (byte)timestamp;

        RandomNumberGenerator.Fill(bytes[6..]);

        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x70);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        return new UuidV7(new Guid(bytes, bigEndian: true));
    }

    public static UuidV7 From(Guid guid) => new(guid);
    public static UuidV7 Parse(string value) => new(Guid.Parse(value));
    public static UuidV7 Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public DateTimeOffset GetTimestamp()
    {
        Span<byte> bytes = stackalloc byte[16];
        Value.TryWriteBytes(bytes, bigEndian: true, out _);
        long ts = ((long)bytes[0] << 40) | ((long)bytes[1] << 32) | ((long)bytes[2] << 24)
                | ((long)bytes[3] << 16) | ((long)bytes[4] << 8) | bytes[5];
        return DateTimeOffset.FromUnixTimeMilliseconds(ts);
    }

    public override string ToString() => Value.ToString();
    public override int GetHashCode() => Value.GetHashCode();
    public override bool Equals(object? obj) => obj is UuidV7 other && Equals(other);
    public bool Equals(UuidV7 other) => Value.Equals(other.Value);
    public int CompareTo(UuidV7 other) => Value.CompareTo(other.Value);

    public static bool operator ==(UuidV7 left, UuidV7 right) => left.Equals(right);
    public static bool operator !=(UuidV7 left, UuidV7 right) => !left.Equals(right);
    public static implicit operator Guid(UuidV7 id) => id.Value;
    public static explicit operator UuidV7(Guid guid) => From(guid);
}
