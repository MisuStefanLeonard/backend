
using E_Commerce_BackEnd.Repositories;


using Microsoft.EntityFrameworkCore.Storage;

namespace E_Commerce_BackEnd.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    IRepository<TEntity> Repository<TEntity>() where TEntity : class;
 

    Task<int> CommitAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task CommitTransactionAsync(IDbContextTransaction dbContextTransaction);
    Task RollBackTransactionAsync(IDbContextTransaction dbContextTransaction);
    
}