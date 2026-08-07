using System.Text.Json;
using Notifications.Application.Interfaces;
using Notifications.Domain.Enums;
using Notifications.Domain.Models;
using ExamPrep.Shared.Constants;

namespace Notifications.Worker.Handlers;

public class PartnerTransactionHandler(
    INotificationDispatcher dispatcher,
    ITemplateService templateService) : IKafkaEventHandler
{
    public string Topic => KafkaTopics.PartnerTransaction;

    public async Task HandleAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        if (!payload.TryGetProperty("PartnerEmail", out var emailProp)) return;

        var recipient = emailProp.GetString();
        var amount = payload.GetProperty("Amount").GetDecimal();
        var type = payload.GetProperty("TransactionType").GetString();
        var desc = payload.GetProperty("Description").GetString();
        var balance = payload.GetProperty("NewBalance").GetDecimal();

        var subject = $"{AppConstants.AppName} Partner Balance Update: {type}";
        var body = await templateService.RenderAsync("PartnerTransaction", new
        {
            type,
            amount = amount.ToString("0.00"),
            desc,
            balance = balance.ToString("0.00"),
            year = DateTime.UtcNow.Year,
            appName = AppConstants.AppName
        });

        var notification = new NotificationMessage(recipient!, subject, body, NotificationType.Email);
        await dispatcher.DispatchAsync(notification, cancellationToken);
    }
}
