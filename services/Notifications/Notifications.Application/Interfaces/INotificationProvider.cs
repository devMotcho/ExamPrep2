using Notifications.Domain.Enums;
using Notifications.Domain.Models;

namespace Notifications.Application.Interfaces;

/// <summary>
/// Defines a provider capable of sending a specific type of notification.
/// To add a new notification type (e.g. SMS, Push), implement this interface
/// and register it in the dependency injection container.
/// </summary>
public interface INotificationProvider
{
    /// <summary>
    /// Gets the type of notification this provider handles.
    /// </summary>
    NotificationType Type { get; }

    /// <summary>
    /// Sends the notification message.
    /// </summary>
    Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}
