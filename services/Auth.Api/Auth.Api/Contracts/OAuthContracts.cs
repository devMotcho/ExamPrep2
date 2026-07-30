using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Contracts;

public record OAuthLoginRequest(
    [Required] string Token
);

public record ConfirmLinkRequest(
    [Required] string LinkTicket,
    [Required] string Password
);
