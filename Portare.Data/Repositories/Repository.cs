using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Portare.Data.Repositories;

public abstract class Repository<TEntity> where TEntity : class
{
    private readonly DbContext _context;
    private readonly ILogger _logger;

    protected Repository(DbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    public virtual async Task<bool> AddAsync(TEntity entity)
    {
        try
        {
            await _context.Set<TEntity>().AddAsync(entity);
            return await _context.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error: Repository.AddAsync");
            return false;
        }
    }
    
    public virtual async Task<bool> UpdateAsync(Expression<Func<TEntity, bool>> predicate, TEntity entity)
    {
        try
        {
            var entityToUpdate = await _context.Set<TEntity>().FirstOrDefaultAsync(predicate);
            if (entityToUpdate == null)
                return false;
            _context.Entry(entityToUpdate).CurrentValues.SetValues(entity);
            return await _context.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error: Repository.UpdateAsync");
            return false;
        }
    }
    
    public virtual async Task<bool> DeleteAsync(Expression<Func<TEntity, bool>> predicate)
    {
        try
        {
            var entityToDelete = await _context.Set<TEntity>().FirstOrDefaultAsync(predicate);
            if (entityToDelete == null)
                return false;
            _context.Set<TEntity>().Remove(entityToDelete);
            return await _context.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error: Repository.DeleteAsync");
            return false;
        }
    }
    
    public virtual async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate)
    {
        try
        {
            return await _context.Set<TEntity>().FirstOrDefaultAsync(predicate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error: Repository.GetAsync");
            return null;
        }
    }
    
    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        try
        {
            return await _context.Set<TEntity>().ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error: Repository.GetAllAsync");
            return new List<TEntity>();
        }
    }
    
    public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate)
    {
        try
        {
            return await _context.Set<TEntity>().AnyAsync(predicate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error: Repository.ExistsAsync");
            return false;
        }
    }
}