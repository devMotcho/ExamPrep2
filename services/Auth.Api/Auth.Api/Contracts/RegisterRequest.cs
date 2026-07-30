using System.ComponentModel.DataAnnotations;
using Auth.Domain.Rules;

namespace Auth.Api.Contracts;

public record RequestEmailVerificationRequest([Required, EmailAddress] string Email);

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required] string Code,
    [Required, MinLength(PasswordRules.MinimumLength)] string Password);