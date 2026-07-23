namespace Auth.Infrastructure.Identity;

public class RefreshToken
{
    public Guid Id { get; set; }
    public string TokenHash { get; set; } = null!;
    public string? Value { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; }

    public string UserId { get; set; } = null!;
    public User User { get; set; } = null!;

}