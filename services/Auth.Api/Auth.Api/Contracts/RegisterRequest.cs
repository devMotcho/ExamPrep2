using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Contracts;

public record RequestEmailVerificationRequest([Required, EmailAddress] string Email);

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required] string Code,
    [Required, MinLength(8)] string Password);