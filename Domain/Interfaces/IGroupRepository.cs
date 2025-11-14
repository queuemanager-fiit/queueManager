using Domain.Entities;

namespace Domain.Interfaces;

public interface IGroupRepository
{
    Task<Group?> GetByCodeAsync(string code, CancellationToken ct);
    Task AddAsync(Group group, CancellationToken ct);
}