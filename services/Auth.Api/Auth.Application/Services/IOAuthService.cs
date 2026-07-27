using Auth.Application.Results;

namespace Auth.Application.Services;

public interface IOAuthService
{
    Task<LoginResult> LoginAsync(string provider, string token);
}
