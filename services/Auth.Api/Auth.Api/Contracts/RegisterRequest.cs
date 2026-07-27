using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Contracts;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password);