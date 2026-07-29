namespace Auth.Application.Models;

public record ExternalUserInfo(
    string Email,
    string Name,
    string Provider,
    string ProviderId,
    bool EmailVerified
);
