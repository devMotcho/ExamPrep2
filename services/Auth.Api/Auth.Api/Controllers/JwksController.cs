using Auth.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Api.Controllers;

[ApiController]
[Route(".well-known")]
public class JwksController(RsaKeyProvider keys) : ControllerBase
{
    private readonly RsaKeyProvider _keys = keys;

    [HttpGet("jwks.json")]
    public IActionResult Get()
    {
        var key = new RsaSecurityKey(_keys.PublicKey) { KeyId = _keys.KeyId };
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(key);
        jwk.Use = "sig";
        jwk.Alg = SecurityAlgorithms.RsaSha256;
        return Ok(new { keys = new[] { jwk } });
    }

}