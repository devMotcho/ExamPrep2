namespace Auth.Application.Interfaces;

/// <summary>Abstraction over a database transaction so Auth.Application
/// never depends on Microsoft.EntityFrameworkCore.</summary>
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync();
    Task RollbackAsync();
}
