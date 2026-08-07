namespace Auth.Application.Models;

/// <summary>Application-level user model. Returned by IUserRepository so the
/// Application layer never depends on Infrastructure entity types.</summary>
public record AppUser(string Id, string Email, DateTime CreatedAt, IReadOnlyList<string> Roles, string? FirstName = null, string? LastName = null, string? PhoneNumber = null);
