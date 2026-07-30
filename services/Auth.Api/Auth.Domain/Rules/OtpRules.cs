namespace Auth.Domain.Rules;

public static class OtpRules
{
    public const int CodeLength = 8;
    
    // 10^8
    public const int MaxOtpValue = 100_000_000;
}
