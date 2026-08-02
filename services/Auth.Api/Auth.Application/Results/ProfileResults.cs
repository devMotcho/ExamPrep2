namespace Auth.Application.Results;

public enum UpdateProfileStatus { Success, ValidationFailed }

public class UpdateProfileResult
{
    public UpdateProfileStatus Status { get; private init; }
    public IEnumerable<string> Errors { get; private init; } = [];

    public static UpdateProfileResult Success() => new() { Status = UpdateProfileStatus.Success };
    public static UpdateProfileResult ValidationFailed(IEnumerable<string> errors) =>
        new() { Status = UpdateProfileStatus.ValidationFailed, Errors = errors };
}

public enum ChangePasswordStatus { Success, IncorrectCurrentPassword, ValidationFailed }

public class ChangePasswordResult
{
    public ChangePasswordStatus Status { get; private init; }
    public IEnumerable<string> Errors { get; private init; } = [];

    public static ChangePasswordResult Success() => new() { Status = ChangePasswordStatus.Success };
    public static ChangePasswordResult IncorrectCurrentPassword() => new() { Status = ChangePasswordStatus.IncorrectCurrentPassword };
    public static ChangePasswordResult ValidationFailed(IEnumerable<string> errors) =>
        new() { Status = ChangePasswordStatus.ValidationFailed, Errors = errors };
}
