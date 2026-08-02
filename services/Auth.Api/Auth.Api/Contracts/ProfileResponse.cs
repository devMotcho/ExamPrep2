namespace Auth.Api.Contracts;

public record ProfileResponse(string Id, string Email, string? FirstName, string? LastName, string? PhoneNumber);
