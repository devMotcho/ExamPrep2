using System.Text.Json;
using Notifications.Application.Interfaces;
using Notifications.Domain.Enums;
using Notifications.Domain.Models;
using ExamPrep.Shared.Constants;

namespace Notifications.Worker.Handlers;

public class PasswordResetHandler(
    INotificationDispatcher dispatcher,
    ITemplateService templateService) : IKafkaEventHandler
{
    public string Topic => KafkaTopics.PasswordResetRequested;

    public async Task HandleAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        if (!payload.TryGetProperty("Email", out var emailProp) ||
            !payload.TryGetProperty("Code", out var codeProp)) return;

        var recipient = emailProp.GetString();
        var code = codeProp.GetString();

        var subject = $"Your {AppConstants.AppName} Password Reset Code";
        var body = await templateService.RenderAsync("PasswordReset", new
        {
            code,
            year = DateTime.UtcNow.Year,
            appName = AppConstants.AppName
        });

        var notification = new NotificationMessage(recipient!, subject, body, NotificationType.Email);
        await dispatcher.DispatchAsync(notification, cancellationToken);
    }
}
