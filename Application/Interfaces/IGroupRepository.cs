using Domain.Entities;

namespace Application.Interfaces;

public interface IGroupRepository : IRepository<Group>
{
    Task<Group?> GetByCodeAsync(string code, CancellationToken ct);
}