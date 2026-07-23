namespace Auth.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore.Storage;

public class UnitOfWork(AuthDbContext db) : IUnitOfWork
{
    public Task<IDbContextTransaction> BeginTransactionAsync() =>
        db.Database.BeginTransactionAsync();

    public Task<int> SaveChangesAsync() => db.SaveChangesAsync();
}