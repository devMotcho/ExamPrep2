using System.ComponentModel.DataAnnotations;
using Auth.Domain.Rules;

namespace Auth.Api.Contracts;

public record ChangePasswordRequest(
    [Required] string CurrentPassword, 
    [Required, MinLength(PasswordRules.MinimumLength)] string NewPassword,
    [Required, StringLength(8, MinimumLength = 8)] string Code);
