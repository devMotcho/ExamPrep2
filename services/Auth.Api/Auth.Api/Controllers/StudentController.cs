using System.Security.Claims;
using Auth.Api.Contracts;
using Auth.Application.Results;
using Auth.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;
[ApiController]
[Route("api/me")]
[Authorize] // any authenticated user — every account is at minimum a Student
public class StudentController(IStudentProfileService profileService) : ControllerBase
{
    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!;

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var user = await profileService.GetProfileAsync(CurrentUserId);
        if (user is null) return NotFound();

        return Ok(new ProfileResponse(user.Id, user.Email, user.FirstName, user.LastName, user.PhoneNumber));
    }

    [HttpPatch]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest req)
    {
        var result = await profileService.UpdateProfileAsync(CurrentUserId, req.FirstName, req.LastName, req.PhoneNumber);

        return result.Status switch
        {
            UpdateProfileStatus.Success => NoContent(),
            UpdateProfileStatus.ValidationFailed => BadRequest(new { errors = result.Errors }),
            _ => throw new InvalidOperationException($"Unhandled status: {result.Status}")
        };
    }

    [HttpPost("change-password/request")]
    public async Task<IActionResult> RequestChangePassword()
    {
        await profileService.RequestChangePasswordCodeAsync(CurrentUserId);
        return Ok(new { message = "A verification code has been sent to your email." });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest req)
    {
        var result = await profileService.ChangePasswordAsync(CurrentUserId, req.CurrentPassword, req.NewPassword, req.Code);

        return result.Status switch
        {
            ChangePasswordStatus.Success => NoContent(),
            ChangePasswordStatus.IncorrectCurrentPassword => BadRequest(new { message = "Current password is incorrect." }),
            ChangePasswordStatus.ValidationFailed => BadRequest(new { errors = result.Errors }),
            ChangePasswordStatus.CodeNotFound => BadRequest(new { message = "Invalid or expired code." }),
            ChangePasswordStatus.CodeInvalid => BadRequest(new { message = "Invalid code." }),
            ChangePasswordStatus.TooManyAttempts => StatusCode(429, new { message = "Too many failed attempts. Please request a new code." }),
            _ => throw new InvalidOperationException($"Unhandled status: {result.Status}")
        };
    }

    [HttpDelete]
    public async Task<IActionResult> Deactivate()
    {
        await profileService.DeactivateAsync(CurrentUserId);
        return NoContent();
    }
}
