namespace Auth.Infrastructure.Security;

public record ExternalUserInfo(string ProviderKey, string Email, string? Name);