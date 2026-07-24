using Auth.Infrastructure.Persistence.Outbox;

namespace Auth.Infrastructure.Repositories;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message);
}