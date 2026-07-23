namespace Auth.Infrastructure.Messaging;

public record UserRegisteredEvent(string Id, string Email, DateTime CreatedAt);