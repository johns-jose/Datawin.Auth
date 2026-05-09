namespace DataWin.Auth.Application.DTOs;

public sealed record ConsentStatusDto
{
    public required Guid UserId { get; init; }
    public required IReadOnlyList<ConsentEntryDto> Consents { get; init; }
}

public sealed record ConsentEntryDto
{
    public required string Purpose { get; init; }
    public required bool IsGranted { get; init; }
    public DateTimeOffset? GrantedAt { get; init; }
    public DateTimeOffset? WithdrawnAt { get; init; }
}
