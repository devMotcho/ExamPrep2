namespace Auth.Application.Results;

/// <summary>Represents the possible outcomes of a logout attempt.</summary>
public enum LogoutStatus
{
    Success,
    /// <summary>No refresh token cookie was provided, or the value was not valid Base64.</summary>
    TokenNotFound,
    /// <summary>The token hash was not found in the database (already revoked or never existed).</summary>
    TokenNotRecognised
}

/// <summary>Encapsulates the result of a logout operation.</summary>
public class LogoutResult
{
    public LogoutStatus Status { get; private init; }

    public static LogoutResult Success() => new() { Status = LogoutStatus.Success };

    public static LogoutResult TokenNotFound() => new() { Status = LogoutStatus.TokenNotFound };

    public static LogoutResult TokenNotRecognised() => new() { Status = LogoutStatus.TokenNotRecognised };
}
