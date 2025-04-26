using System.Linq.Expressions;
using E_Commerce_BackEnd.Models.Context;

using Microsoft.EntityFrameworkCore;

namespace E_Commerce_BackEnd.Repositories;

public class GenericRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
    private readonly ECommerceContext _context;
    private readonly DbSet<TEntity> _dbSet;

    public GenericRepository(ECommerceContext context)
    {
        _context = context;
        _dbSet = _context.Set<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }
    

    public async Task<IList<TEntity>?> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }
    
    public async Task AddAsync(TEntity entity)
    {
        await _dbSet.AddAsync(entity);
    }
    
    public async Task UpdateAsync(TEntity entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(TEntity entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public IQueryable<TEntity> GetSimpleQueryable()
    {
        return _dbSet.AsQueryable();
    }

    public async Task<TEntity?> FindEntityWithJoinAsync(
        Expression<Func<TEntity, bool>> expression,
        params Expression<Func<TEntity, object>>[] navigationProps)
    {
        var filteredByCondition = _dbSet.Where(expression);

        foreach (var navigationProp in navigationProps)
        {
            filteredByCondition = filteredByCondition.Include(navigationProp);
        }

        return await filteredByCondition.FirstOrDefaultAsync();
    }


    public async Task<IQueryable<TEntity>?> FindQueryableOfEntitiesAsync(Expression<Func<TEntity, bool>> expression, 
        params Expression<Func<TEntity, object>>[] navigationProps)
    {
        var filteredCollection = _dbSet.Where(expression);

        filteredCollection = navigationProps.Aggregate(filteredCollection, (current, navigationProp) => current.Include(navigationProp));

        await Task.CompletedTask;

        return filteredCollection;
    }

    public async Task DeleteRangeAsync(IList<TEntity> listOfEntities)
    {
        _dbSet.RemoveRange(listOfEntities);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IList<TEntity> listOfEntities)
    {
        await _dbSet.AddRangeAsync(listOfEntities);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRangeAsync(IList<TEntity> listOfEntities)
    {
        _dbSet.UpdateRange();
        await _context.SaveChangesAsync();
    }
    
    public IQueryable<TEntity> FindQueryable(Expression<Func<TEntity, bool>> expression, 
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null)
    {
        var query = _dbSet.Where(expression);
        return orderBy != null ? orderBy(query) : query;
    }
}