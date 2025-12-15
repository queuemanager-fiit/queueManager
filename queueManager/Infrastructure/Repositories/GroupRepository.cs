using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class GroupRepository : BaseRepository<Group>, IGroupRepository
{
    public GroupRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Group?> GetByCodeAsync(string code, CancellationToken ct)
    {
        return await Context.Groups.FirstOrDefaultAsync(x => x.Code == code, ct);
    }

    public async Task<List<Group>> GetGroupsByCategoryAsync(Guid categoryId, CancellationToken ct)
    {
        var category = await Context.EventCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId, ct);
        if (category?.GroupCode == null) return null;
        return await Context.Groups
            .FirstOrDefaultAsync(g => g.Code == category.GroupCode, ct);
    }
}