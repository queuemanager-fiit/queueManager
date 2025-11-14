using Domain.Entities;

namespace Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    Task UpdateAsync(User user, CancellationToken ct);
}