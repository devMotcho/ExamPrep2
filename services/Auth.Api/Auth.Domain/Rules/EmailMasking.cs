namespace Auth.Domain.Rules;

public static class EmailMasking
{
    public static string Mask(string email)
    {
        var atIndex = email.IndexOf('@');
        return atIndex <= 2 ? email : $"{email[..2]}***{email[atIndex..]}";
    }
}
