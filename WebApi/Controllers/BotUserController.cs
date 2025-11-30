using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Application.Interfaces;
using Domain.Entities;

namespace WebApi.Controllers;

[ApiController]
[Route("api/users")]
public sealed class BotUserController : ControllerBase
{
    private readonly IUserRepository users;
    private readonly IGroupRepository groups;
    private readonly IUnitOfWork uow;

    public BotUserController(IUserRepository users, IGroupRepository groups, IUnitOfWork uow)
    {
        this.users = users;
        this.groups = groups;
        this.uow = uow;
    }

    public sealed record BotUserDto(
        [property: Required] string FullName,
        [property: Required, StringLength(32)] string Username,
        [property: Required] string GroupCode,
        [property: Required] long TelegramId);

    [HttpPost("update-userinfo")]
    public async Task<IActionResult> UpdateUserInfo([FromBody] BotUserDto dto, CancellationToken ct)
    {
        var group = await groups.GetByCodeAsync(dto.GroupCode, ct);
        
        if (group is null)
        {
            group = new Group(dto.GroupCode);
            await groups.AddAsync(group, ct);
        }
        
        var user = await users.GetByTelegramIdAsync(dto.TelegramId, ct);
        if (user is null)
        {
            user = new User(dto.FullName, dto.Username, dto.TelegramId, group.Id);
            await users.AddAsync(user, ct);
        }
        else
        {
            user.UpdateInfo(dto.FullName, dto.Username, group.Id);
            await users.UpdateAsync(user, ct);
        }

        await uow.SaveChangesAsync(ct);
        return NoContent();
    }
    
    [HttpPost("add-user")]
    public async Task<IActionResult> AddUser(CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}