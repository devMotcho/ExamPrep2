using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Contracts;

public record VerifyEmailVerificationRequest(
    [Required, EmailAddress] string Email, 
    [Required, StringLength(8, MinimumLength = 8, ErrorMessage = "Code must be exactly 8 characters.")] string Code);
