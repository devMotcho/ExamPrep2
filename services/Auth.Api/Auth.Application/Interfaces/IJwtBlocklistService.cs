namespace Auth.Application.Interfaces;

public interface IJwtBlocklistService
{
    Task BlockTokenAsync(string jti, TimeSpan timeToLive);
    Task<bool> IsTokenBlockedAsync(string jti);
}
