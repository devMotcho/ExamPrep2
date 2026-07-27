namespace Auth.Application.Results;

public enum RegisterStatus
{
    Success,
    EmailAlreadyRegistered,
    ValidationFailed
}

public class RegisterResult
{
    public RegisterStatus Status { get; private init; }
    public string? AccessToken { get; private init; }
    public string? RawRefreshToken { get; private init; }
    public IEnumerable<string> Errors { get; private init; } = [];

    public static RegisterResult Success(string accessToken, string rawRefreshToken) => new()
    {
        Status = RegisterStatus.Success,
        AccessToken = accessToken,
        RawRefreshToken = rawRefreshToken
    };

    public static RegisterResult EmailAlreadyRegistered() => new()
    {
        Status = RegisterStatus.EmailAlreadyRegistered
    };

    public static RegisterResult ValidationFailed(IEnumerable<string> errors) => new()
    {
        Status = RegisterStatus.ValidationFailed,
        Errors = errors
    };
}
