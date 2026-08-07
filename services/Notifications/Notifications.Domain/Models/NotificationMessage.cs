using Notifications.Domain.Enums;

namespace Notifications.Domain.Models;

public class NotificationMessage
{
    public NotificationMessage(string recipient, string subject, string body, NotificationType type)
    {
        Id = Guid.NewGuid();
        Recipient = recipient;
        Subject = subject;
        Body = body;
        Type = type;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; }
    public string Recipient { get; }
    public string Subject { get; }
    public string Body { get; }
    public NotificationType Type { get; }
    public DateTime CreatedAt { get; }
}
