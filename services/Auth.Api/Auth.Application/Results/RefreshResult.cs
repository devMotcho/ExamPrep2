namespace Auth.Application.Results;

public enum RefreshStatus
{
    Success,
    TokenNotFound,
    TokenExpiredOrRevoked
}

public class RefreshResult
{
    public RefreshStatus Status { get; private init; }
    public string? AccessToken { get; private init; }
    public string? RawRefreshToken { get; private init; }

    public static RefreshResult Success(string accessToken, string rawRefreshToken) => new()
    {
        Status = RefreshStatus.Success,
        AccessToken = accessToken,
        RawRefreshToken = rawRefreshToken
    };

    public static RefreshResult TokenNotFound() => new()
    {
        Status = RefreshStatus.TokenNotFound
    };

    public static RefreshResult TokenExpiredOrRevoked() => new()
    {
        Status = RefreshStatus.TokenExpiredOrRevoked
    };
}
