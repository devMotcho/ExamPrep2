namespace Auth.Infrastructure.Outbox;

public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Topic { get; set; } = null!;
    public string Key { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}