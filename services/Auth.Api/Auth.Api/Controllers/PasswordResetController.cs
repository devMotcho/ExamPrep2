using Auth.Api.Contracts;
using Auth.Application.Results;
using Auth.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

/// <summary>
/// Handles the three-step password-reset flow.
/// </summary>
[ApiController]
[Route("api/auth/password-reset")]
public class PasswordResetController(IPasswordResetService resetService) : ControllerBase
{
    /// <summary>
    /// Step 1: Requests a password-reset code for the given email.
    /// </summary>
    /// <param name="req">The request containing the email address.</param>
    /// <returns>Always returns 200 OK to prevent account enumeration.</returns>
    /// <response code="200">Request processed.</response>
    [HttpPost("request")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestReset(PasswordResetRequestRequest req)
    {
        await resetService.RequestAsync(req.Email);
        return Ok(new { message = "If the email is registered, a reset code has been sent." });
    }

    /// <summary>
    /// Step 2: Verifies the 8-digit OTP code sent to the user's email.
    /// </summary>
    /// <param name="req">The request containing the email and the code.</param>
    /// <returns>A short-lived reset ticket on success.</returns>
    /// <response code="200">Code verified. Returns a reset ticket.</response>
    /// <response code="400">Invalid code, code expired, or code locked due to too many attempts.</response>
    [HttpPost("verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Verify(PasswordResetVerifyRequest req)
    {
        var result = await resetService.VerifyAsync(req.Email, req.Code);

        return result.Status switch
        {
            PasswordResetVerifyStatus.Success => Ok(new { resetTicket = result.ResetTicket }),
            PasswordResetVerifyStatus.CodeNotFound => BadRequest(new { message = "Invalid or expired code." }),
            PasswordResetVerifyStatus.CodeInvalid => BadRequest(new { message = "Invalid code." }),
            PasswordResetVerifyStatus.TooManyAttempts => BadRequest(new { message = "Too many failed attempts. Please request a new code." }),
            _ => throw new InvalidOperationException($"Unhandled verify status: {result.Status}")
        };
    }

    /// <summary>
    /// Step 3: Completes the password reset by providing the ticket and the new password.
    /// </summary>
    /// <param name="req">The request containing the reset ticket and the new password.</param>
    /// <returns>200 OK on success.</returns>
    /// <response code="200">Password successfully reset.</response>
    /// <response code="400">Invalid/expired ticket or password validation failed.</response>
    [HttpPost("confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Confirm(PasswordResetConfirmRequest req)
    {
        var result = await resetService.ConfirmAsync(req.ResetTicket, req.NewPassword);

        return result.Status switch
        {
            PasswordResetConfirmStatus.Success => Ok(new { message = "Password successfully reset." }),
            PasswordResetConfirmStatus.TicketInvalid => BadRequest(new { message = "Invalid or expired reset ticket." }),
            PasswordResetConfirmStatus.PasswordValidationFailed => BadRequest(new { errors = result.Errors }),
            _ => throw new InvalidOperationException($"Unhandled confirm status: {result.Status}")
        };
    }
}
