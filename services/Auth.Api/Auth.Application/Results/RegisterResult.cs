namespace Auth.Application.Results;

/// <summary>Represents the possible outcomes of a user registration attempt.</summary>
public enum RegisterStatus 
{ 
    Success, 
    EmailAlreadyRegistered, 
    ValidationFailed, 
    InvalidOrExpiredCode, 
    TooManyAttempts 
}

/// <summary>Encapsulates the result of a user registration operation.</summary>
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

    public static RegisterResult InvalidOrExpiredCode() => new() { Status = RegisterStatus.InvalidOrExpiredCode };
    public static RegisterResult TooManyAttempts() => new() { Status = RegisterStatus.TooManyAttempts };
}
