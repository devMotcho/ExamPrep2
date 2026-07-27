namespace Auth.Application.Results;

public enum LoginStatus
{
    Success,
    InvalidCredentials
}

public class LoginResult
{
    public LoginStatus Status { get; private init; }
    public string? AccessToken { get; private init; }
    public string? RawRefreshToken { get; private init; }

    public static LoginResult Success(string accessToken, string rawRefreshToken) => new()
    {
        Status = LoginStatus.Success,
        AccessToken = accessToken,
        RawRefreshToken = rawRefreshToken
    };

    /// <summary>Returned for both wrong password and unknown user — callers cannot
    /// distinguish which, preventing user-enumeration attacks.</summary>
    public static LoginResult InvalidCredentials() => new()
    {
        Status = LoginStatus.InvalidCredentials
    };
}
