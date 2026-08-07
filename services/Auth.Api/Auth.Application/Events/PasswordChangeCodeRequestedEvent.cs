namespace Auth.Application.Events;

public record PasswordChangeCodeRequestedEvent(string UserId, string Email, string Code);
