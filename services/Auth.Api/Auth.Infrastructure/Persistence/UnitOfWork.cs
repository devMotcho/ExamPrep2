using Auth.Application.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Auth.Infrastructure.Persistence;

/// <summary>Adapts EF Core's IDbContextTransaction to the Application-layer
/// ITransaction so Auth.Application never references EF Core directly.</summary>
internal sealed class EfTransaction(IDbContextTransaction inner) : ITransaction
{
    public Task CommitAsync() => inner.CommitAsync();
    public Task RollbackAsync() => inner.RollbackAsync();
    public ValueTask DisposeAsync() => inner.DisposeAsync();
}

public class UnitOfWork(AuthDbContext db) : IUnitOfWork
{
    public async Task<ITransaction> BeginTransactionAsync()
    {
        var tx = await db.Database.BeginTransactionAsync();
        return new EfTransaction(tx);
    }

    public Task<int> SaveChangesAsync() => db.SaveChangesAsync();
}