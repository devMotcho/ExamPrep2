using System.ComponentModel.DataAnnotations;
using Auth.Domain.Rules;

namespace Auth.Api.Contracts;

public record RequestEmailVerificationRequest([Required, EmailAddress] string Email);

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, StringLength(8, MinimumLength = 8, ErrorMessage = "Code must be exactly 8 characters.")] string Code,
    [Required, MinLength(PasswordRules.MinimumLength)] string Password,
    string? PartnerEmail = null);