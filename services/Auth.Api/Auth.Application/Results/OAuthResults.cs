namespace Auth.Application.Results;

public enum LoginStatus { Success, InvalidCredentials, AccountLinkRequired }

public class LoginResult
{
    public LoginStatus Status { get; private init; }
    public string? AccessToken { get; private init; }
    public string? RawRefreshToken { get; private init; }
    public string? LinkTicket { get; private init; }
    public string? MaskedEmail { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static LoginResult Success(string accessToken, string rawRefreshToken) =>
        new() { Status = LoginStatus.Success, AccessToken = accessToken, RawRefreshToken = rawRefreshToken };

    public static LoginResult InvalidCredentials(string message = "Invalid credentials") =>
        new() { Status = LoginStatus.InvalidCredentials, ErrorMessage = message };

    public static LoginResult AccountLinkRequired(string linkTicket, string maskedEmail) =>
        new() { Status = LoginStatus.AccountLinkRequired, LinkTicket = linkTicket, MaskedEmail = maskedEmail };
}

public enum ConfirmLinkStatus { Success, InvalidOrExpiredTicket, InvalidPassword }

public class ConfirmLinkResult
{
    public ConfirmLinkStatus Status { get; private init; }
    public string? AccessToken { get; private init; }
    public string? RawRefreshToken { get; private init; }

    public static ConfirmLinkResult Success(string accessToken, string rawRefreshToken) =>
        new() { Status = ConfirmLinkStatus.Success, AccessToken = accessToken, RawRefreshToken = rawRefreshToken };

    public static ConfirmLinkResult InvalidOrExpiredTicket() => new() { Status = ConfirmLinkStatus.InvalidOrExpiredTicket };
    public static ConfirmLinkResult InvalidPassword() => new() { Status = ConfirmLinkStatus.InvalidPassword };
}
