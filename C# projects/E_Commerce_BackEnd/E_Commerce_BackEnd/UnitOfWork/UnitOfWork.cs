using AutoMapper;
using E_Commerce_BackEnd.Models.Context;
using E_Commerce_BackEnd.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace E_Commerce_BackEnd.UnitOfWork;

public class UnitOfWork : IUnitOfWork 
{
    private readonly ECommerceContext _context;

    public UnitOfWork(ECommerceContext context, IMapper mapper)
    {
        _context = context;
        
    }

    public IRepository<TEntity> Repository<TEntity>() where TEntity : class
    {
        return new GenericRepository<TEntity>(_context);
    }
    
    

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync(IDbContextTransaction transaction)
    {
        try
        {
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task RollBackTransactionAsync(IDbContextTransaction transaction)
    {
        await transaction.RollbackAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}