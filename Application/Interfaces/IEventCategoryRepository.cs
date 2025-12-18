using Domain.Entities;

namespace Application.Interfaces;

public interface IEventCategoryRepository : IRepository<EventCategory>
{
    Task<List<EventCategory>> GetAutoCreateCategoriesAsync(CancellationToken ct);
    Task<EventCategory?> GetByGroupIdAndNameAsync(string groupCode, string name, CancellationToken ct);
}