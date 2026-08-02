namespace Auth.Domain.Rules;

public static class Roles
{
    public const string Student = "Student";
    public const string Promoter = "Promoter";
    public const string Admin = "Admin";

    public static readonly string[] All = [Student, Promoter, Admin];

    /// <summary>
    /// Roles that can never be removed from a user once assigned.
    /// Enforced in AdminUserService, not just documented here.
    /// </summary>
    public static readonly string[] Protected = [Student];
}
