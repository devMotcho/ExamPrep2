using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Contracts;

public record LoginRequest(
    [Required] string EmailOrUsername,
    [Required] string Password);
