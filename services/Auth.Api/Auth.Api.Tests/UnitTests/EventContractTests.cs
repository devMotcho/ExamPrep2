using System.Text.Json;
using Auth.Infrastructure.Messaging;

namespace Auth.Api.Tests.UnitTests;

public class EventContractTests
{
    [Fact]
    public void UserRegisteredEvent_SerializesWithExpectedFieldNames()
    {
        var evt = new UserRegisteredEvent(Guid.NewGuid().ToString(), "person@example.com", DateTime.UtcNow);

        var json = JsonSerializer.Serialize(evt);
        using var doc = JsonDocument.Parse(json);

        // These exact names must match contracts/events/user-registered.v1.md,
        // and must match what the Debezium connector config's
        // transforms.outbox.table.field.event.* values expect.
        Assert.True(doc.RootElement.TryGetProperty("Id", out _));
        Assert.True(doc.RootElement.TryGetProperty("Email", out _));
        Assert.True(doc.RootElement.TryGetProperty("CreatedAt", out _));
    }
}