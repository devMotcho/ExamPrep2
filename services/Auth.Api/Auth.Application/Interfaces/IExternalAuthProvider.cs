using Auth.Application.Models;

namespace Auth.Application.Interfaces;

public interface IExternalAuthProvider
{
    string ProviderName { get; }
    Task<ExternalUserInfo?> ValidateTokenAsync(string token);
}
