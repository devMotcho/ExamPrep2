using Auth.Application.Interfaces;
using Auth.Infrastructure.Persistence;
using Auth.Infrastructure.Persistence.Outbox;

namespace Auth.Infrastructure.Repositories;

public class OutboxRepository(AuthDbContext db) : IOutboxRepository
{
    public Task AddAsync(string topic, string key, string payload)
    {
        db.OutboxMessages.Add(new OutboxMessage
        {
            Topic = topic,
            Key = key,
            Payload = payload
        });
        return Task.CompletedTask;
    }
}