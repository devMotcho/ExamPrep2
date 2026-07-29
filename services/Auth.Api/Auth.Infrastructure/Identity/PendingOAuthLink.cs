namespace Auth.Infrastructure.Identity;

public class PendingOAuthLink
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    public User User { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string ProviderKey { get; set; } = null!;
    public string TicketHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsUsed { get; set; }
    public int Attempts { get; set; }
}
