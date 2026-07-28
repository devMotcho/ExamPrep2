using Auth.Application.Results;

namespace Auth.Application.Services;

/// <summary>
/// Orchestrates the three-step password-reset flow:
/// <list type="number">
///   <item>
///     <description>
///       <b>Request</b> — generates an 8-digit OTP, stores its hash, and publishes
///       a <c>password-reset-requested</c> event for Notification.Api to email the code.
///       Always returns success to prevent account enumeration.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Verify</b> — validates the OTP against the stored hash, enforces expiry and
///       attempt limits, and on success issues a short-lived single-use reset ticket.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Confirm</b> — validates the reset ticket, sets the new password via
///       ASP.NET Identity, and revokes all existing refresh tokens to invalidate every
///       active session (session-fixation protection).
///     </description>
///   </item>
/// </list>
/// </summary>
public interface IPasswordResetService
{
    /// <summary>
    /// Initiates the password-reset flow for the account associated with
    /// <paramref name="email"/>. Always returns <see cref="PasswordResetRequestResult.Success()"/>
    /// regardless of whether the email is registered.
    /// </summary>
    /// <param name="email">The email address of the account to reset.</param>
    Task<PasswordResetRequestResult> RequestAsync(string email);

    /// <summary>
    /// Verifies the 8-digit OTP code against the stored hash for the given email.
    /// On success, invalidates the code and returns a short-lived reset ticket.
    /// </summary>
    /// <param name="email">The email address the code was sent to.</param>
    /// <param name="code">The 8-digit code entered by the user.</param>
    /// <returns>
    /// <see cref="PasswordResetVerifyResult"/> carrying the reset ticket on success,
    /// or a failure status indicating expiry, wrong code, or too many attempts.
    /// </returns>
    Task<PasswordResetVerifyResult> VerifyAsync(string email, string code);

    /// <summary>
    /// Completes the reset by setting <paramref name="newPassword"/> for the account
    /// identified by <paramref name="rawResetTicket"/>, then revokes all refresh tokens
    /// to terminate every active session.
    /// </summary>
    /// <param name="rawResetTicket">The reset ticket returned by <see cref="VerifyAsync"/>.</param>
    /// <param name="newPassword">The desired new password (validated by ASP.NET Identity rules).</param>
    /// <returns>
    /// <see cref="PasswordResetConfirmResult"/> indicating success, an invalid/expired
    /// ticket, or Identity password-validation failures.
    /// </returns>
    Task<PasswordResetConfirmResult> ConfirmAsync(string rawResetTicket, string newPassword);
}
