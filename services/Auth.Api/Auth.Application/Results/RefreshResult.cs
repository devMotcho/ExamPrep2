namespace Auth.Application.Results;

/// <summary>Represents the possible outcomes of a token refresh attempt.</summary>
public enum RefreshStatus
{
    Success,
    TokenNotFound,
    TokenExpiredOrRevoked
}

/// <summary>Encapsulates the result of a token refresh operation.</summary>
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
