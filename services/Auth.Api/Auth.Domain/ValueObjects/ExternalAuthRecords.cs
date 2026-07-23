namespace Auth.Domain.ValueObjects;

public record ExternalUserInfo(string ProviderKey, string Email, string? Name);