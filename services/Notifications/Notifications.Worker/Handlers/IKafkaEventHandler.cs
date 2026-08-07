using System.Text.Json;

namespace Notifications.Worker.Handlers;

/// <summary>
/// Handles a specific Kafka event topic by transforming its payload
/// into one or more notification dispatches.
/// </summary>
public interface IKafkaEventHandler
{
    /// <summary>The Kafka topic this handler is responsible for.</summary>
    string Topic { get; }

    /// <summary>
    /// Processes a single parsed event payload.
    /// </summary>
    /// <param name="payload">The unwrapped JSON payload (Debezium envelope already stripped).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleAsync(JsonElement payload, CancellationToken cancellationToken);
}
