using Domain.Entities;

namespace Application.Interfaces;

public interface IEventRepository : IRepository<Event>
{
    /// <summary>
    /// Возвращает все события, по которым пришло время отправить уведомление.
    /// </summary>
    Task<List<Event>> GetDueNotificationAsync(DateTimeOffset now, CancellationToken ct);
    
    Task<List<Event>> GetDueFormationAsync(DateTimeOffset now, CancellationToken ct);
    
    Task<Event?> GetByIdAsync(Guid eventId, CancellationToken ct);
}