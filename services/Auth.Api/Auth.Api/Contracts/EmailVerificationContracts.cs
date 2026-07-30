using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Contracts;

public record EmailVerificationRequestRequest(
    [Required, EmailAddress] string Email
);

public record EmailVerificationVerifyRequest(
    [Required, EmailAddress] string Email,
    [Required, StringLength(Auth.Domain.Rules.OtpRules.CodeLength, MinimumLength = Auth.Domain.Rules.OtpRules.CodeLength)] string Code
);
