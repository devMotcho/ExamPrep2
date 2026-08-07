using System.Text.Json;
using Confluent.Kafka;
using Notifications.Worker.Handlers;
using ExamPrep.Shared.Constants;

namespace Notifications.Worker;

/// <summary>
/// Kafka consumer loop responsible only for:
///   1. Connecting to Kafka and subscribing to topics declared by registered handlers
///   2. Unwrapping the Debezium envelope
///   3. Routing the clean payload to the correct <see cref="IKafkaEventHandler"/>
/// 
/// All event-specific business logic lives in individual handler classes.
/// </summary>
public class KafkaConsumerBackgroundService : BackgroundService
{
    private readonly ILogger<KafkaConsumerBackgroundService> _logger;
    private readonly IConfiguration _config;
    private readonly IReadOnlyDictionary<string, IKafkaEventHandler> _handlers;

    public KafkaConsumerBackgroundService(
        ILogger<KafkaConsumerBackgroundService> logger,
        IConfiguration config,
        IEnumerable<IKafkaEventHandler> handlers)
    {
        _logger = logger;
        _config = config;
        _handlers = handlers.ToDictionary(h => h.Topic);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _config[ConfigKeys.Kafka.BootstrapServers] ?? "localhost:9092",
            GroupId = _config[ConfigKeys.Kafka.GroupId] ?? "notifications-worker-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();

        var topics = _handlers.Keys.ToArray();
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

                    await ProcessMessageAsync(consumeResult.Topic, consumeResult.Message.Value, stoppingToken);
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
            var payload = UnwrapDebeziumPayload(json);

            if (_handlers.TryGetValue(topic, out var handler))
            {
                await handler.HandleAsync(payload, cancellationToken);
            }
            else
            {
                _logger.LogWarning("No handler registered for topic {Topic}", topic);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Kafka message on topic {Topic}", topic);
        }
    }

    /// <summary>
    /// Strips the Debezium envelope (if present) and returns the inner event payload.
    /// Handles three formats:
    ///   1. Debezium wrapped: { "payload": { "after": { "Payload": "..." } } }
    ///   2. Primitive string wrapper: "\"{ ... }\""
    ///   3. Raw JSON object (no wrapping)
    /// </summary>
    private static JsonElement UnwrapDebeziumPayload(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);

            // Format 1: Debezium CDC with Outbox Event Router
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("payload", out var payloadElement) &&
                payloadElement.ValueKind == JsonValueKind.Object &&
                payloadElement.TryGetProperty("after", out var afterElement) &&
                afterElement.ValueKind == JsonValueKind.Object &&
                afterElement.TryGetProperty("Payload", out var eventPayloadElement))
            {
                return JsonDocument.Parse(eventPayloadElement.GetString()!).RootElement;
            }

            // Format 2: Primitive string (Event Router dumped the string directly)
            if (doc.RootElement.ValueKind == JsonValueKind.String)
            {
                return JsonDocument.Parse(doc.RootElement.GetString()!).RootElement;
            }

            // Format 3: Already the raw parsed object
            return doc.RootElement;
        }
        catch (JsonException)
        {
            // Fallback: raw string payload not wrapped in JSON quotes
            return JsonDocument.Parse(json).RootElement;
        }
    }
}
