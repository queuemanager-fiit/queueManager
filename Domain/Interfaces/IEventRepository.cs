using Domain.Entities;

namespace Domain.Interfaces;

public interface IEventRepository : IRepository<Event>
{
    /// <summary>
    /// Возвращает все события, по которым пришло время отправить уведомление.
    /// </summary>
    Task<List<Event>> GetDueAsync(DateTimeOffset now, CancellationToken ct);
    
    Task<List<Event>> GetForUserAsync(long telegramId, CancellationToken ct);
    
    Task MarkAsNotifiedAsync(IEnumerable<Guid> ids, DateTimeOffset now, CancellationToken ct);
}