using Auth.Application.Results;

namespace Auth.Application.Services;

/// <summary>
/// Orchestrates the two-step email verification flow:
/// <list type="number">
///   <item>
///     <description>
///       <b>Request</b> — generates an 8-digit OTP, stores its hash, and publishes
///       an <c>email-verification-requested</c> event for Notification.Api to email the code.
///       Always returns success for unknown emails to prevent account enumeration.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Verify</b> — validates the OTP against the stored hash, enforces expiry and
///       attempt limits, and on success marks the user's email as confirmed via ASP.NET Identity.
///     </description>
///   </item>
/// </list>
/// </summary>
public interface IEmailVerificationService
{
    /// <summary>
    /// Initiates the email verification flow for the account associated with
    /// <paramref name="email"/>. Always returns <see cref="EmailVerificationRequestResult.Success()"/>
    /// regardless of whether the email is registered.
    /// </summary>
    Task<EmailVerificationRequestResult> RequestAsync(string email);

    /// <summary>
    /// Verifies the 8-digit OTP code against the stored hash for the given email.
    /// On success, marks the email as confirmed and invalidates the code.
    /// </summary>
    Task<EmailVerificationVerifyResult> VerifyAsync(string email, string code);
}
