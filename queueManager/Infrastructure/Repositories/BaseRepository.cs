using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public abstract class BaseRepository<T> : IRepository<T>
    where T : class
{
    protected readonly ApplicationDbContext Context;

    protected BaseRepository(ApplicationDbContext context)
    {
        Context = context;
    }

    public virtual async Task AddAsync(T entity, CancellationToken ct)
    {
        await Context.AddAsync(entity, ct);
    }

    public virtual Task UpdateAsync(T entity, CancellationToken ct)
    {
        Context.Update(entity);
        return Task.CompletedTask;
    }
    
    public virtual async Task DeleteAsync(T entity, CancellationToken ct)
    {
        Context.Set<T>().Remove(entity);
        await Context.SaveChangesAsync(ct);
    }
}
