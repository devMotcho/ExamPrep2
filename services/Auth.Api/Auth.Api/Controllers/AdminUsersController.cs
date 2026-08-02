using Auth.Application.Results;
using Auth.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Auth.Domain.Rules;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = Roles.Admin)]
public class AdminUsersController(IAdminUserService adminUserService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (users, total) = await adminUserService.SearchUsersAsync(q, page, pageSize);
        return Ok(new { users, total, page, pageSize });
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> Get(string userId)
    {
        var user = await adminUserService.GetUserAsync(userId);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost("{userId}/roles/{role}")]
    public async Task<IActionResult> AssignRole(string userId, string role)
    {
        var result = await adminUserService.AssignRoleAsync(userId, role);
        return result.Status switch
        {
            AssignRoleStatus.Success => NoContent(),
            AssignRoleStatus.UserNotFound => NotFound(),
            AssignRoleStatus.UnknownRole => BadRequest(new { message = "Unknown role." }),
            _ => throw new InvalidOperationException($"Unhandled status: {result.Status}")
        };
    }

    [HttpDelete("{userId}/roles/{role}")]
    public async Task<IActionResult> RemoveRole(string userId, string role)
    {
        var result = await adminUserService.RemoveRoleAsync(userId, role);
        return result.Status switch
        {
            RemoveRoleStatus.Success => NoContent(),
            RemoveRoleStatus.UserNotFound => NotFound(),
            RemoveRoleStatus.UnknownRole => BadRequest(new { message = "Unknown role." }),
            RemoveRoleStatus.RoleIsProtected => BadRequest(new { message = "This role cannot be removed." }),
            RemoveRoleStatus.LastAdminCannotBeRemoved => Conflict(new { message = "Cannot remove the last remaining admin." }),
            _ => throw new InvalidOperationException($"Unhandled status: {result.Status}")
        };
    }

    [HttpPost("{userId}/deactivate")]
    public async Task<IActionResult> Deactivate(string userId)
    {
        await adminUserService.DeactivateUserAsync(userId);
        return NoContent();
    }
}
