using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Domain.Repositories;

public class EventRepository : BaseRepository<Event>, IEventRepository
{
    public EventRepository(ApplicationDbContext context) : base(context) { }

    public async Task<List<Event>> GetDueAsync(DateTimeOffset now, CancellationToken ct)
    {
        return await Context.Events
            .Where(e => e.OccurredOn <= now)
            .Include(e => e.Users)
            .ToListAsync(ct);
    }

    public Task<List<Event>> GetForUserAsync(long telegramId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<Event?> GetByIdAsync(Guid eventId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task MarkAsNotifiedAsync(IEnumerable<Guid> ids, DateTimeOffset now, CancellationToken ct)
    {
        var events = await Context.Events
            .Where(e => ids.Contains(e.Id))
            .ToListAsync(ct);
            
        Context.Events.RemoveRange(events);
    }
}