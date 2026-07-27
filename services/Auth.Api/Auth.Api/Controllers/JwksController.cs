using Auth.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Api.Controllers;

[ApiController]
[Route(".well-known")]
public class JwksController(IJwksProvider keys) : ControllerBase
{
    [HttpGet("jwks.json")]
    public IActionResult Get()
    {
        var key = new RsaSecurityKey(keys.PublicKey) { KeyId = keys.KeyId };
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(key);
        jwk.Use = "sig";
        jwk.Alg = SecurityAlgorithms.RsaSha256;
        return Ok(new { keys = new[] { jwk } });
    }
}