using Application.Interfaces;
using Domain.Entities;

namespace Domain.Interfaces;

public interface IEventCategoryRepository : IRepository<EventCategory>
{
    Task<List<EventCategory>> GetAutoCreateCategoriesAsync(CancellationToken ct);
    Task<EventCategory?> GetByNameAsync(string name, CancellationToken ct);
    Task<EventCategory?> GetByGroupIdAndNameAsync(Guid groupId, string name, CancellationToken ct);
}