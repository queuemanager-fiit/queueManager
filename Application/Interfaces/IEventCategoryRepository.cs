using Domain.Entities;

namespace Application.Interfaces;

public interface IEventCategoryRepository : IRepository<EventCategory>
{
    Task<List<EventCategory>> GetAutoCreateCategoriesAsync(CancellationToken ct);
    
    /// <summary>
    /// Возвращает категорию события по коду группы и названию категории.
    /// </summary>
    Task<EventCategory?> GetByGroupIdAndNameAsync(string groupCode, string name, CancellationToken ct);
    
    Task<EventCategory?> GetByIdAsync(Guid id, CancellationToken ct);
}