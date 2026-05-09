namespace DataWin.Auth.Core.ValueObjects;

public sealed record HashedPassword
{
    public required string Hash { get; init; }
    public required string Algorithm { get; init; }

    public const string Argon2Id = "argon2id";
}
