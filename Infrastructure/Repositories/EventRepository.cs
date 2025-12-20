using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EventRepository : BaseRepository<Event>, IEventRepository
{
    public EventRepository(ApplicationDbContext context) : base(context) { }

    public async Task<List<Event>> GetDueNotificationAsync(DateTimeOffset now, CancellationToken ct)
    {
        return await Context.Events
            .Where(e => e.OccurredOn <= now)
            .Include(e => e.Participants)
            .ToListAsync(ct);
    }

    public async Task<List<Event>> GetDueFormationAsync(DateTimeOffset now, CancellationToken ct)
    {
        return await Context.Events
            .Where(e => e.FormationTime <= now)
            .Include(e => e.Participants)
            .ToListAsync(ct);
    }

    public async Task<Event?> GetByIdAsync(Guid eventId, CancellationToken ct)
    {
        return await Context.Events
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == eventId, ct);
    }

    public async Task MarkAsNotifiedAsync(IEnumerable<Guid> ids, DateTimeOffset now, CancellationToken ct)
    {
        var events = await Context.Events
            .Where(e => ids.Contains(e.Id))
            .ToListAsync(ct);
            
        foreach (var eventItem in events)
        {
            eventItem.MarkAsNotified(now); // ← ИСПОЛЬЗУЙ МЕТОД!
        }
        
        Context.Events.UpdateRange(events);
    }
    
    public async Task MarkAsFormedAsync(IEnumerable<Guid> ids, DateTimeOffset now, CancellationToken ct)
    {
        var events = await Context.Events
            .Where(e => ids.Contains(e.Id))
            .ToListAsync(ct);
            
        foreach (var eventItem in events)
        {
            eventItem.MarkAsFormed(now); // ← ИСПОЛЬЗУЙ МЕТОД!
        }
        
        Context.Events.UpdateRange(events);
    }
}