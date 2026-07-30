using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Contracts;

public record PasswordResetRequestRequest(
    [Required, EmailAddress] string Email
);

public record PasswordResetVerifyRequest(
    [Required, EmailAddress] string Email,
    [Required, StringLength(Auth.Domain.Rules.OtpRules.CodeLength, MinimumLength = Auth.Domain.Rules.OtpRules.CodeLength)] string Code
);

public record PasswordResetConfirmRequest(
    [Required] string ResetTicket,
    [Required] string NewPassword
);
