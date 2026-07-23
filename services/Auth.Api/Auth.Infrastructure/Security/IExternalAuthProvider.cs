using Auth.Domain.ValueObjects;

namespace Auth.Infrastructure.Security;

public interface IExternalAuthProvider
{
    string ProviderName { get; }
    Task<ExternalUserInfo> ValidateAsync(string providerToken);
}