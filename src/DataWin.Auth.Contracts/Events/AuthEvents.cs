namespace DataWin.Auth.Contracts.Events;

public interface IAuthEvent
{
    Guid EventId { get; }
    string EventType { get; }
    Guid TenantId { get; }
    DateTimeOffset Timestamp { get; }
}

public sealed record AuthEventEnvelope : IAuthEvent
{
    public required Guid EventId { get; init; }
    public required string EventType { get; init; }
    public required Guid TenantId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required IDictionary<string, object> Payload { get; init; }
}
