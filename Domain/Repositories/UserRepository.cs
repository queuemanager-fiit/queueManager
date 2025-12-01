using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Domain.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public async Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken ct)
    {
        return await Context.Users.FirstOrDefaultAsync(x => x.TelegramId == telegramId, cancellationToken: ct);
    }
}