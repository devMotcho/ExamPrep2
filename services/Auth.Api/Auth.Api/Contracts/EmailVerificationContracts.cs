using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Contracts;

public record EmailVerificationRequestRequest(
    [Required, EmailAddress] string Email
);

public record EmailVerificationVerifyRequest(
    [Required, EmailAddress] string Email,
    [Required, StringLength(8, MinimumLength = 8)] string Code
);
