using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Events;

public interface IDomainEvent
{
    UuidV7 EventId { get; }
    DateTimeOffset OccurredAt { get; }
    UuidV7 TenantId { get; }
}

public sealed record UserAuthenticatedEvent(
    UuidV7 EventId, DateTimeOffset OccurredAt, UuidV7 TenantId,
    UuidV7 UserId, string IpAddress, bool MfaUsed) : IDomainEvent;

public sealed record MfaEnrolledEvent(
    UuidV7 EventId, DateTimeOffset OccurredAt, UuidV7 TenantId,
    UuidV7 UserId, string Method) : IDomainEvent;

public sealed record ConsentGrantedEvent(
    UuidV7 EventId, DateTimeOffset OccurredAt, UuidV7 TenantId,
    UuidV7 UserId, string Purpose) : IDomainEvent;

public sealed record ConsentWithdrawnEvent(
    UuidV7 EventId, DateTimeOffset OccurredAt, UuidV7 TenantId,
    UuidV7 UserId, string Purpose) : IDomainEvent;

public sealed record ErasureRequestedEvent(
    UuidV7 EventId, DateTimeOffset OccurredAt, UuidV7 TenantId,
    UuidV7 UserId, UuidV7 RequestId) : IDomainEvent;

public sealed record UserLockedOutEvent(
    UuidV7 EventId, DateTimeOffset OccurredAt, UuidV7 TenantId,
    UuidV7 UserId, string Reason) : IDomainEvent;
