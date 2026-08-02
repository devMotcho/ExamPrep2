using System.ComponentModel.DataAnnotations;
using Auth.Domain.Rules;

namespace Auth.Api.Contracts;

public record ChangePasswordRequest(
    [Required] string CurrentPassword, 
    [Required, MinLength(PasswordRules.MinimumLength)] string NewPassword);
