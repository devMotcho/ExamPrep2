using Auth.Api.Contracts;
using Auth.Application.Results;
using Auth.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/auth/email-verification")]
public class EmailVerificationController(IEmailVerificationService verificationService) : ControllerBase
{
    /// <summary>
    /// Step 1: Requests an email verification code for the given email.
    /// </summary>
    [HttpPost("request")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestVerification(EmailVerificationRequestRequest req)
    {
        var result = await verificationService.RequestAsync(req.Email);

        if (result.Status == EmailVerificationRequestStatus.AlreadyVerified)
            return Ok(new { message = "Email is already verified." });

        return Ok(new { message = "If the email is registered and unverified, a verification code has been sent." });
    }

    /// <summary>
    /// Step 2: Verifies the 8-digit OTP code sent to the user's email.
    /// </summary>
    [HttpPost("verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Verify(EmailVerificationVerifyRequest req)
    {
        var result = await verificationService.VerifyAsync(req.Email, req.Code);

        return result.Status switch
        {
            EmailVerificationVerifyStatus.Success => Ok(new { message = "Email successfully verified." }),
            EmailVerificationVerifyStatus.AlreadyVerified => Ok(new { message = "Email is already verified." }),
            EmailVerificationVerifyStatus.CodeNotFound => BadRequest(new { message = "Invalid or expired code." }),
            EmailVerificationVerifyStatus.CodeInvalid => BadRequest(new { message = "Invalid code." }),
            EmailVerificationVerifyStatus.TooManyAttempts => BadRequest(new { message = "Too many failed attempts. Please request a new code." }),
            _ => throw new InvalidOperationException($"Unhandled verify status: {result.Status}")
        };
    }
}
