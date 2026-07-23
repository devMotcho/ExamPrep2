namespace Auth.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore.Storage;

public interface IUnitOfWork
{
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task<int> SaveChangesAsync();
}