using Auth.Infrastructure.Persistence;
using Auth.Infrastructure.Persistence.Outbox;

namespace Auth.Infrastructure.Repositories;

public class OutboxRepository(AuthDbContext db) : IOutboxRepository
{
    public Task AddAsync(OutboxMessage message)
    {
        db.OutboxMessages.Add(message);
        return Task.CompletedTask;
    }
}