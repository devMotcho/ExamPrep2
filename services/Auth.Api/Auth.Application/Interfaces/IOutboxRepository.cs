namespace Auth.Application.Interfaces;

public interface IOutboxRepository
{
    Task AddAsync(string topic, string key, string payload);
}
