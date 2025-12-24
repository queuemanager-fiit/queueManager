using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EventCategoryRepository : BaseRepository<EventCategory>, IEventCategoryRepository
{
    public EventCategoryRepository(ApplicationDbContext context) : base(context) { }

    public async Task<List<EventCategory>> GetAutoCreateCategoriesAsync(CancellationToken ct)
    {
        return await Context.EventCategories
            .Where(ec => ec.IsAutoCreate)
            .ToListAsync(ct);
    }

    public async Task<EventCategory?> GetByGroupIdAndNameAsync(string groupCode, string name, CancellationToken ct)
    {
        return await Context.EventCategories
            .FirstOrDefaultAsync(ec =>
                ec.GroupCode == groupCode &&
                ec.SubjectName == name,
                ct);
    }

    public async Task<EventCategory?> GetBySubjectNameAsync(string subjectName, CancellationToken ct)
    {
        return await Context.EventCategories
            .FirstOrDefaultAsync(ec => ec.SubjectName == subjectName, ct);
    }

    public async Task<bool> ExistsAsync(string subjectName, CancellationToken ct)
    {
        return await Context.EventCategories
            .AnyAsync(ec => ec.SubjectName == subjectName, ct);
    }

    public async Task<List<EventCategory>> GetByGroupCodeAsync(string groupCode, CancellationToken ct)
    {
        return await Context.EventCategories
            .Where(ec => ec.GroupCode == groupCode)
            .ToListAsync(ct);
    }
    public async Task<EventCategory?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await Context.EventCategories
            .FirstOrDefaultAsync(ec => ec.Id == id, ct);
    }
}
