namespace Auth.Application.Events;

/// <summary>Domain event raised when a new user successfully registers.
/// Serialised into the outbox and routed to Kafka by Debezium.</summary>
public record UserRegisteredEvent(string Id, string Email, DateTime CreatedAt);
