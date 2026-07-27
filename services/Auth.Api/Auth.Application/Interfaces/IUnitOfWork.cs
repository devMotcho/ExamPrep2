namespace Auth.Application.Interfaces;

public interface IUnitOfWork
{
    Task<ITransaction> BeginTransactionAsync();
    Task<int> SaveChangesAsync();
}
