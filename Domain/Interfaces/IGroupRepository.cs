using Domain.Entities;

namespace Domain.Interfaces;

public interface IGroupRepository : IRepository<Group>
{
    Task<Group?> GetByCodeAsync(string code, CancellationToken ct);
}