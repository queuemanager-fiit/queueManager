using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EventRepository : BaseRepository<Event>, IEventRepository
{
    public EventRepository(ApplicationDbContext context) : base(context) { }

    public async Task<List<Event>> GetDueNotificationAsync(DateTimeOffset now, CancellationToken ct)
    {
        return await Context.Set<Event>()
            .Where(e => e.NotificationTime <= now && !e.IsNotified)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<List<Event>> GetDueFormationAsync(DateTimeOffset now, CancellationToken ct)
    {
        return await Context.Set<Event>()
            .Where(e => e.FormationTime <= now && !e.IsFormed)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<List<Event>> GetExpiredEventsAsync(DateTimeOffset now, CancellationToken ct)
    {
        return await Context.Set<Event>()
            .Where(e => e.DeletionTime <= now)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await Context.Set<Event>()
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<List<Event>> GetByIdsAsync(List<Guid> ids, CancellationToken ct)
    {
        if (ids == null || !ids.Any())
            return new List<Event>();
        
        return await Context.Set<Event>()
            .Where(e => ids.Contains(e.Id))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
