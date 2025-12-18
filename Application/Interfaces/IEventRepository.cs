using Domain.Entities;

namespace Application.Interfaces;

public interface IEventRepository : IRepository<Event>
{
    /// <summary>
    /// Возвращает все события, по которым пришло время отправить уведомление.
    /// </summary>
    Task<List<Event>> GetDueAsync(DateTimeOffset now, CancellationToken ct);
    
    Task<Event?> GetByIdAsync(Guid eventId, CancellationToken ct);
}