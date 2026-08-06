using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notifications.Application.Interfaces;
using Notifications.Domain.Enums;
using Notifications.Domain.Models;

namespace Notifications.Worker;

public class KafkaConsumerBackgroundService : BackgroundService
{
    private readonly ILogger<KafkaConsumerBackgroundService> _logger;
    private readonly IConfiguration _config;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ITemplateService _templateService;

    public KafkaConsumerBackgroundService(
        ILogger<KafkaConsumerBackgroundService> logger,
        IConfiguration config,
        INotificationDispatcher dispatcher,
        ITemplateService templateService)
    {
        _logger = logger;
        _config = config;
        _dispatcher = dispatcher;
        _templateService = templateService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = _config["Kafka:GroupId"] ?? "notifications-worker-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        
        var topics = new[] { "partner-transaction", "email-verification-code-requested", "password-change-code-requested" };
        consumer.Subscribe(topics);

        _logger.LogInformation("Kafka consumer started listening to topics: {Topics}", string.Join(", ", topics));

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    if (consumeResult?.Message == null) continue;

                    var json = consumeResult.Message.Value;
                    var topic = consumeResult.Topic;
                    await ProcessMessageAsync(topic, json, stoppingToken);
                }
                catch (ConsumeException e)
                {
                    _logger.LogWarning("Kafka consume warning: {Reason}. Retrying in 5 seconds...", e.Error.Reason);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka consumer is stopping.");
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task ProcessMessageAsync(string topic, string json, CancellationToken cancellationToken)
    {
        try
        {
            // Parse Debezium payload structure
            // If the Outbox Event Router converts the record to a primitive JSON string (due to schema.enable=false and string column)
            // The json might just be the direct payload string (with escaped quotes). Let's unescape if necessary.
            
            JsonElement root;
            try 
            {
                var doc = JsonDocument.Parse(json);
                // If it's a Debezium wrapped object with "payload" -> "after" -> "payload"
                if (doc.RootElement.ValueKind == JsonValueKind.Object && 
                    doc.RootElement.TryGetProperty("payload", out var payloadElement) &&
                    payloadElement.ValueKind == JsonValueKind.Object &&
                    payloadElement.TryGetProperty("after", out var afterElement) &&
                    afterElement.ValueKind == JsonValueKind.Object &&
                    afterElement.TryGetProperty("Payload", out var eventPayloadElement))
                {
                    root = JsonDocument.Parse(eventPayloadElement.GetString()!).RootElement;
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.String)
                {
                    // It's a primitive string (Event Router dumped the string directly)
                    root = JsonDocument.Parse(doc.RootElement.GetString()!).RootElement;
                }
                else
                {
                    // It's already the raw parsed object
                    root = doc.RootElement;
                }
            }
            catch (JsonException)
            {
                // Fallback if the json is exactly the raw string payload but not wrapped in JSON quotes
                root = JsonDocument.Parse(json).RootElement;
            }

            if (topic == "partner-transaction")
            {
                if (root.TryGetProperty("PartnerEmail", out var emailProp))
                {
                    var recipient = emailProp.GetString();
                    var amount = root.GetProperty("Amount").GetDecimal();
                    var type = root.GetProperty("TransactionType").GetString();
                    var desc = root.GetProperty("Description").GetString();
                    var balance = root.GetProperty("NewBalance").GetDecimal();

                    var subject = $"Partner Balance Update: {type}";
                    var body = await _templateService.RenderAsync("PartnerTransaction", new {
                        type = type,
                        amount = amount.ToString("0.00"),
                        desc = desc,
                        balance = balance.ToString("0.00"),
                        year = DateTime.UtcNow.Year
                    });

                    var notification = new NotificationMessage(recipient!, subject, body, NotificationType.Email);
                    await _dispatcher.DispatchAsync(notification, cancellationToken);
                }
            }
            else if (topic == "email-verification-code-requested")
            {
                if (root.TryGetProperty("Email", out var emailProp) && root.TryGetProperty("Code", out var codeProp))
                {
                    var recipient = emailProp.GetString();
                    var code = codeProp.GetString();

                    var subject = "Your ExamPrep Verification Code";
                    var body = await _templateService.RenderAsync("EmailVerification", new { 
                        code = code, 
                        year = DateTime.UtcNow.Year 
                    });

                    var notification = new NotificationMessage(recipient!, subject, body, NotificationType.Email);
                    await _dispatcher.DispatchAsync(notification, cancellationToken);
                }
            }
            else if (topic == "password-change-code-requested")
            {
                if (root.TryGetProperty("Email", out var emailProp) && root.TryGetProperty("Code", out var codeProp))
                {
                    var recipient = emailProp.GetString();
                    var code = codeProp.GetString();

                    var subject = "Your ExamPrep Password Change Code";
                    var body = await _templateService.RenderAsync("PasswordChange", new { 
                        code = code, 
                        year = DateTime.UtcNow.Year 
                    });

                    var notification = new NotificationMessage(recipient!, subject, body, NotificationType.Email);
                    await _dispatcher.DispatchAsync(notification, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Kafka message on topic {Topic}", topic);
        }
    }
}
