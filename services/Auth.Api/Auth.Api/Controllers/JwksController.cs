using Auth.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Api.Controllers;

[ApiController]
[Route(".well-known")]
/// <summary>
/// Provides the JSON Web Key Set (JWKS) used to verify JWT access tokens.
/// </summary>
public class JwksController(IJwksProvider keys) : ControllerBase
{
    /// <summary>
    /// Retrieves the public keys in JWKS format.
    /// </summary>
    /// <returns>JSON representing the keys.</returns>
    /// <response code="200">Returns the JWKS payload.</response>
    [HttpGet("jwks.json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        var key = new RsaSecurityKey(keys.PublicKey) { KeyId = keys.KeyId };
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(key);
        jwk.Use = "sig";
        jwk.Alg = SecurityAlgorithms.RsaSha256;
        return Ok(new { keys = new[] { jwk } });
    }
}