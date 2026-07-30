using System.ComponentModel.DataAnnotations;
using Auth.Domain.Rules;

namespace Auth.Api.Contracts;

public record PasswordResetRequestRequest(
    [Required, EmailAddress] string Email
);

public record PasswordResetVerifyRequest(
    [Required, EmailAddress] string Email,
    [Required, StringLength(OtpRules.CodeLength, MinimumLength = OtpRules.CodeLength)] string Code
);

public record PasswordResetConfirmRequest(
    [Required] string ResetTicket,
    [Required] string NewPassword
);
