using Notifications.Application.Interfaces;
using Notifications.Domain.Models;

namespace Notifications.Application.Services;

public class NotificationDispatcher : INotificationDispatcher
{
    private readonly IEnumerable<INotificationProvider> _providers;

    public NotificationDispatcher(IEnumerable<INotificationProvider> providers)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
    }

    public async Task DispatchAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var provider = _providers.FirstOrDefault(p => p.Type == message.Type);
        
        if (provider == null)
        {
            throw new NotSupportedException($"No notification provider found for type '{message.Type}'.");
        }

        await provider.SendAsync(message, cancellationToken);
    }
}
