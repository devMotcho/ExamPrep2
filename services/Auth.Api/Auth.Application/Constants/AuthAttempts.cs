namespace Auth.Application.Constants;

public static class AuthAttempts
{ 
    /// <summary>
    /// Maximum failed verify attempts before the code is considered locked.
    /// </summary>
    public const int MaxLinkAttempts = 5;

    /// <summary>
    /// Maximum failed verify attempts before the code is considered locked.
    /// </summary>
    public const int MaxResetPasswordAttempts = 5;
    
    /// <summary>
    /// Maximum failed verify attempts before the email verification code is considered locked.
    /// </summary>
    public const int MaxEmailVerificationCodeAttempts = 5;
}