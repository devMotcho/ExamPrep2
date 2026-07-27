using Auth.Application.Results;

namespace Auth.Application.Services;

public interface IAuthService
{
    Task<RegisterResult> RegisterAsync(string email, string password);
    Task<RefreshResult> RefreshAsync(string rawRefreshToken);
}
