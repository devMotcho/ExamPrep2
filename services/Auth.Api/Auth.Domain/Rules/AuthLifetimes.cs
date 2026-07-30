namespace Auth.Domain.Rules;

public static class AuthLifetimes
{
    public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);
    public static readonly TimeSpan EmailVerificationCodeLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan PasswordResetCodeLifetime = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan PasswordResetTicketLifetime = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan LinkTicketLifetime = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan LoginLockoutLifetime = TimeSpan.FromMinutes(15);

    public const int MaxCodeAttempts = 5;
    public const int MaxLinkAttempts = 5;
    public const int MaxLoginAttempts = 5;
}
