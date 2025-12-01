using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Domain.Repositories;

public class GroupRepository : BaseRepository<Group>, IGroupRepository
{
    public GroupRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Group?> GetByCodeAsync(string code, CancellationToken ct)
    {
        return await Context.Groups.FirstOrDefaultAsync(x => x.Code == code, ct);
    }

    public Task<List<Group>> GetGroupsByCategoryAsync(Guid categoryId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}