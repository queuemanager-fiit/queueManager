using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class GroupRepository : BaseRepository<Group>, IGroupRepository
{
    public GroupRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Group?> GetByCodeAsync(string code, CancellationToken ct)
    {
        return await Context.Groups.FirstOrDefaultAsync(g => g.Code == code, ct);
    }
}