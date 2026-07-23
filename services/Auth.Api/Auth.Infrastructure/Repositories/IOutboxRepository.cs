using Auth.Infrastructure.Outbox;

namespace Auth.Infrastructure.Repositories;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message);
}