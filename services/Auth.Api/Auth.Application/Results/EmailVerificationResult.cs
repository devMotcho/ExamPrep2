namespace Auth.Application.Results;

// ── Request ──────────────────────────────────────────────────────────────────

public enum EmailVerificationRequestStatus { Success, AlreadyVerified }

public class EmailVerificationRequestResult
{
    public EmailVerificationRequestStatus Status { get; private init; }

    /// <summary>
    /// Returns success regardless of whether the email exists, to prevent account enumeration.
    /// However, if the user is already verified, it may optionally return AlreadyVerified.
    /// </summary>
    public static EmailVerificationRequestResult Success() => new() { Status = EmailVerificationRequestStatus.Success };
    
    public static EmailVerificationRequestResult AlreadyVerified() => new() { Status = EmailVerificationRequestStatus.AlreadyVerified };
}

// ── Verify ────────────────────────────────────────────────────────────────────

public enum EmailVerificationVerifyStatus
{
    Success,
    /// <summary>No active code exists for this email, or it has expired.</summary>
    CodeNotFound,
    /// <summary>The supplied code does not match the stored hash.</summary>
    CodeInvalid,
    /// <summary>Too many failed attempts — the code has been locked.</summary>
    TooManyAttempts,
    /// <summary>The user's email is already verified.</summary>
    AlreadyVerified
}

public class EmailVerificationVerifyResult
{
    public EmailVerificationVerifyStatus Status { get; private init; }

    public static EmailVerificationVerifyResult Success() => new() { Status = EmailVerificationVerifyStatus.Success };
    public static EmailVerificationVerifyResult CodeNotFound() => new() { Status = EmailVerificationVerifyStatus.CodeNotFound };
    public static EmailVerificationVerifyResult CodeInvalid() => new() { Status = EmailVerificationVerifyStatus.CodeInvalid };
    public static EmailVerificationVerifyResult TooManyAttempts() => new() { Status = EmailVerificationVerifyStatus.TooManyAttempts };
    public static EmailVerificationVerifyResult AlreadyVerified() => new() { Status = EmailVerificationVerifyStatus.AlreadyVerified };
}
