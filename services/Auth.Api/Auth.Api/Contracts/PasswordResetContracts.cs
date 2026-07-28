using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Contracts;

public record PasswordResetRequestRequest(
    [Required, EmailAddress] string Email
);

public record PasswordResetVerifyRequest(
    [Required, EmailAddress] string Email,
    [Required, StringLength(8, MinimumLength = 8)] string Code
);

public record PasswordResetConfirmRequest(
    [Required] string ResetTicket,
    [Required] string NewPassword
);
