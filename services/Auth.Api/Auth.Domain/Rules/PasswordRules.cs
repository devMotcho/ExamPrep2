namespace Auth.Domain.Rules;

public static class PasswordRules
{
    public const int MinimumLength = 8;
    public const bool RequireNonAlphanumeric = true;
}
