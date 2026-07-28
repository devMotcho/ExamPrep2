namespace Auth.Application.Events;

/// <summary>
/// Domain event raised when an email verification OTP is generated.
/// Serialised into the outbox and routed to Kafka by Debezium.
/// Notification.Api consumes this event to email the code to the user.
/// </summary>
/// <param name="UserId">The requesting user's identifier.</param>
/// <param name="Email">The address to send the code to.</param>
/// <param name="Code">The raw (un-hashed) 8-digit code to embed in the email.</param>
public record EmailVerificationRequestedEvent(string UserId, string Email, string Code);
