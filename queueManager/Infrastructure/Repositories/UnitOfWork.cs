using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext Сontext;
    
    public UnitOfWork(ApplicationDbContext context)
    {
        Сontext = context;
    }
    
    public async Task<int> SaveChangesAsync(CancellationToken ct)
    {
        return await Сontext.SaveChangesAsync(ct);
    }
}