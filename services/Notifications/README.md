# Notifications Microservice

This service is responsible for consuming asynchronous events (via Kafka) and dispatching notifications to users (Email, SMS, Push, etc.). It strictly adheres to Clean Architecture and SOLID principles, ensuring maximum scalability and separation of concerns.

## Architecture

- **Domain (`Notifications.Domain`)**: Contains the core `NotificationMessage` model and the `NotificationType` enum.
- **Application (`Notifications.Application`)**: Contains interfaces (`INotificationProvider`, `INotificationDispatcher`) and the `NotificationDispatcher` which orchestrates routing messages to the correct infrastructure provider.
- **Infrastructure (`Notifications.Infrastructure`)**: Contains specific implementations for delivering notifications (e.g., `SmtpEmailProvider` using Google Mail servers).
- **Worker (`Notifications.Worker`)**: The entry point. A Background Hosted Service (`KafkaConsumerBackgroundService`) that constantly listens for events on Kafka topics, formats them into `NotificationMessage` objects, and passes them to the Dispatcher.

## How to Add a New Notification Type (e.g., SMS)

Because this service is built with an object-oriented Open/Closed Principle approach, you can easily add a new notification type (like SMS or Push) without modifying existing dispatching logic.

### Step 1: Update the Domain
Add your new type to the `NotificationType` enum in `Notifications.Domain/Enums/NotificationType.cs`:
```csharp
public enum NotificationType
{
    Email = 1,
    Sms = 2
}
```

### Step 2: Implement the Provider (Infrastructure)
Create a new provider class in `Notifications.Infrastructure/Providers` that implements `INotificationProvider`.
```csharp
public class TwilioSmsProvider : INotificationProvider
{
    public NotificationType Type => NotificationType.Sms;

    public async Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        // Add your Twilio (or other SMS provider) logic here!
    }
}
```

### Step 3: Register the Provider (Worker)
Register your new provider in the Dependency Injection container inside `Notifications.Worker/Program.cs`:
```csharp
// The dispatcher will automatically pick this up!
services.AddSingleton<INotificationProvider, TwilioSmsProvider>();
```

### Step 4: Dispatch the Notification
When the Kafka consumer parses an event that requires an SMS, simply instantiate a `NotificationMessage` with `NotificationType.Sms` and dispatch it:
```csharp
var notification = new NotificationMessage(phoneNumber, subject, body, NotificationType.Sms);
await _dispatcher.DispatchAsync(notification, cancellationToken);
```

The `NotificationDispatcher` automatically routes the message to the correct provider!

## Running the Service
Ensure you have the following in your `appsettings.json` or Environment Variables:
```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "GroupId": "notifications-worker-group",
    "Topic": "partner-transaction"
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "Username": "your.email@gmail.com",
    "Password": "your-app-password",
    "FromAddress": "no-reply@escolhamultipla.pt"
  }
}
```
