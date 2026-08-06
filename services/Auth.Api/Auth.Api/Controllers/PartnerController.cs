using System.IdentityModel.Tokens.Jwt;
using Auth.Application.Interfaces;
using Auth.Domain.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/partners")]
public class PartnerController(IPartnerService partnerService) : ControllerBase
{
    /// <summary>
    /// Gets all information about the partner, including balance and transaction history.
    /// Only accessible by users with the Partner role.
    /// </summary>
    [HttpGet("me")]
    [Authorize(Roles = Roles.Partner)]
    [ProducesResponseType(typeof(PartnerInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyInfo()
    {
        var userId = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier || c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var info = await partnerService.GetPartnerInfoAsync(userId);
        if (info is null) return NotFound();

        return Ok(info);
    }

    /// <summary>
    /// Admin endpoint to manually subtract funds from a partner's bank (e.g. payout).
    /// </summary>
    [HttpPost("{partnerId}/subtract-balance")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubtractBalance(string partnerId, [FromBody] SubtractBalanceRequest req)
    {
        var result = await partnerService.SubtractBalanceAsync(partnerId, req.Amount, req.Description);
        if (!result) return BadRequest(new { message = "Failed to subtract balance. Check partner ID and ensure they have sufficient funds." });

        return Ok(new { message = "Balance subtracted successfully." });
    }
}

public record SubtractBalanceRequest(decimal Amount, string Description);
