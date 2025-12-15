using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EventRepository : BaseRepository<Event>, IEventRepository
{
    public EventRepository(ApplicationDbContext context) : base(context) { }

    public async Task<List<Event>> GetDueAsync(DateTimeOffset now, CancellationToken ct)
    {
        return await Context.Events
            .Where(e => e.OccurredOn <= now)
            .Include("Participants")
            .ToListAsync(ct);
    }

    public async Task<List<Event>> GetForUserAsync(long telegramId, CancellationToken ct)
    {
        return await Context.Events
            .Where(e => e.Participants.Any(p => p.TelegramId == telegramId))
            .ToListAsync(ct);
    }
    public async Task<Event?> GetByIdAsync(Guid eventId, CancellationToken ct)
    {
        return await Context.Events.FirstOrDefaultAsync(x => x.Id == eventId, ct);
    }

    public async Task<List<Event>> GetCreatedByAsync(long telegramId, CancellationToken ct)
    {
        return new List<Event>();
    }
}