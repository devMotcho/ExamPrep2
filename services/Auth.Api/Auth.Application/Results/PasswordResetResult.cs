namespace Auth.Application.Results;

// ── Request ──────────────────────────────────────────────────────────────────

public enum PasswordResetRequestStatus { Success }

public class PasswordResetRequestResult
{
    public PasswordResetRequestStatus Status { get; private init; }

    /// <summary>
    /// Always returns <see cref="PasswordResetRequestStatus.Success"/> regardless of
    /// whether the email exists — callers must not expose different responses to the
    /// client, preventing account-enumeration attacks.
    /// </summary>
    public static PasswordResetRequestResult Success() => new()
    {
        Status = PasswordResetRequestStatus.Success
    };
}

// ── Verify ────────────────────────────────────────────────────────────────────

public enum PasswordResetVerifyStatus
{
    Success,
    /// <summary>No active code exists for this email, or it has expired.</summary>
    CodeNotFound,
    /// <summary>The supplied code does not match the stored hash.</summary>
    CodeInvalid,
    /// <summary>Too many failed attempts — the code has been locked.</summary>
    TooManyAttempts
}

public class PasswordResetVerifyResult
{
    public PasswordResetVerifyStatus Status { get; private init; }

    /// <summary>
    /// The raw (un-hashed) reset ticket to be returned to the client.
    /// Only populated on <see cref="PasswordResetVerifyStatus.Success"/>.
    /// </summary>
    public string? ResetTicket { get; private init; }

    public static PasswordResetVerifyResult Success(string resetTicket) => new()
    {
        Status = PasswordResetVerifyStatus.Success,
        ResetTicket = resetTicket
    };

    public static PasswordResetVerifyResult CodeNotFound() => new()
    {
        Status = PasswordResetVerifyStatus.CodeNotFound
    };

    public static PasswordResetVerifyResult CodeInvalid() => new()
    {
        Status = PasswordResetVerifyStatus.CodeInvalid
    };

    public static PasswordResetVerifyResult TooManyAttempts() => new()
    {
        Status = PasswordResetVerifyStatus.TooManyAttempts
    };
}

// ── Confirm ───────────────────────────────────────────────────────────────────

public enum PasswordResetConfirmStatus
{
    Success,
    /// <summary>The ticket was not found, is already used, or has expired.</summary>
    TicketInvalid,
    /// <summary>The new password failed ASP.NET Identity validation rules.</summary>
    PasswordValidationFailed
}

public class PasswordResetConfirmResult
{
    public PasswordResetConfirmStatus Status { get; private init; }
    public IEnumerable<string> Errors { get; private init; } = [];

    public static PasswordResetConfirmResult Success() => new()
    {
        Status = PasswordResetConfirmStatus.Success
    };

    public static PasswordResetConfirmResult TicketInvalid() => new()
    {
        Status = PasswordResetConfirmStatus.TicketInvalid
    };

    public static PasswordResetConfirmResult PasswordValidationFailed(IEnumerable<string> errors) => new()
    {
        Status = PasswordResetConfirmStatus.PasswordValidationFailed,
        Errors = errors
    };
}
