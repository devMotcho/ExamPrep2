namespace Auth.Application.Constants;

public static class AuthLifetimes
{
    /// <summary>How long a generated OTP refresh token remains valid.</summary>
    public static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(15);

    /// <summary>How long the link ticket remains valid.</summary>
    public static readonly TimeSpan LinkTicketLifetime = TimeSpan.FromMinutes(8);
     
    /// <summary>How long a generated OTP code remains valid.</summary>
    public static readonly TimeSpan ResetPasswordCodeLifetime = TimeSpan.FromMinutes(8);
     
    /// <summary>How long the reset ticket remains valid after a successful verify.</summary>
    public static readonly TimeSpan ResetPasswordTicketLifetime = TimeSpan.FromMinutes(5);

    /// <summary>How long the email verification code remains valid after a successful code request.</summary>
    public static readonly TimeSpan EmailVerificationCodeLifetime = TimeSpan.FromMinutes(8);
}