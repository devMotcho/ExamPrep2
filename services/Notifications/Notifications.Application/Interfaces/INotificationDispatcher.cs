using System.Threading;
using System.Threading.Tasks;
using Notifications.Domain.Models;

namespace Notifications.Application.Interfaces;

/// <summary>
/// Orchestrates the routing of notification messages to their appropriate providers.
/// </summary>
public interface INotificationDispatcher
{
    /// <summary>
    /// Dispatches a notification to the correct provider based on its type.
    /// </summary>
    Task DispatchAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}
