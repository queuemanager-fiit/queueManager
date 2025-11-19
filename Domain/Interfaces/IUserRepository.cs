using Domain.Entities;

namespace Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken ct);
}