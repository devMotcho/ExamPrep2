namespace Auth.Application.Results;

public enum AssignRoleStatus { Success, UserNotFound, UnknownRole }
public enum RemoveRoleStatus { Success, UserNotFound, UnknownRole, RoleIsProtected, LastAdminCannotBeRemoved }

public class AssignRoleResult
{
    public AssignRoleStatus Status { get; private init; }
    public static AssignRoleResult Success() => new() { Status = AssignRoleStatus.Success };
    public static AssignRoleResult UserNotFound() => new() { Status = AssignRoleStatus.UserNotFound };
    public static AssignRoleResult UnknownRole() => new() { Status = AssignRoleStatus.UnknownRole };
}

public class RemoveRoleResult
{
    public RemoveRoleStatus Status { get; private init; }
    public static RemoveRoleResult Success() => new() { Status = RemoveRoleStatus.Success };
    public static RemoveRoleResult UserNotFound() => new() { Status = RemoveRoleStatus.UserNotFound };
    public static RemoveRoleResult UnknownRole() => new() { Status = RemoveRoleStatus.UnknownRole };
    public static RemoveRoleResult RoleIsProtected() => new() { Status = RemoveRoleStatus.RoleIsProtected };
    public static RemoveRoleResult LastAdminCannotBeRemoved() => new() { Status = RemoveRoleStatus.LastAdminCannotBeRemoved };
}
