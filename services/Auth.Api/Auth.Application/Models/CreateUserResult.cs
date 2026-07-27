namespace Auth.Application.Models;

/// <summary>Outcome of IUserRepository.CreateAsync — decoupled from
/// IdentityResult so the Application layer has no Identity dependency.</summary>
public class CreateUserResult
{
    public bool Succeeded { get; private init; }
    public AppUser? User { get; private init; }
    public IEnumerable<string> Errors { get; private init; } = [];

    public static CreateUserResult Success(AppUser user) => new()
    {
        Succeeded = true,
        User = user
    };

    public static CreateUserResult Failure(IEnumerable<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };
}
