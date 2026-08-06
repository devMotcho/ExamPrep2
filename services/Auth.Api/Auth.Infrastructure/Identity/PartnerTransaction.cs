namespace Auth.Infrastructure.Identity;

public enum TransactionType
{
    Addition = 1,
    Subtraction = 2
}

public class PartnerTransaction
{
    public Guid Id { get; set; }
    public string PartnerId { get; set; } = string.Empty;
    public User? Partner { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = string.Empty;
}
