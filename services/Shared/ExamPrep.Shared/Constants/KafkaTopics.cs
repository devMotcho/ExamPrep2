namespace ExamPrep.Shared.Constants;

public static class KafkaTopics
{
    public const string EmailVerificationCodeRequested = "email-verification-code-requested";
    public const string PasswordChangeCodeRequested = "password-change-code-requested";
    public const string PasswordResetRequested = "password-reset-requested";
    public const string PartnerTransaction = "partner-transaction";
    public const string UserRegistered = "user-registered";
}
