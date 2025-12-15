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
        [Required] string FullName,
        [Required, StringLength(32)] string Username,
        [Required] string GroupCode,
        [Required] string SubGroupCode,
        [Required] long TelegramId);

    //вызывается при первом обращении пользователя, чтобы зарегистрировать, затем раз в определенный срок для обновления информации (смена ника в тг, смена группы и т.д.)
    [HttpPost("update-userinfo")]
    public async Task<IActionResult> UpdateUserInfo([FromBody] BotUserDto dto, CancellationToken ct)
    {
        var group = await groups.GetByCodeAsync(dto.GroupCode, ct);
        
        if (group is null)
        {
            group = new Group(dto.GroupCode);
            var subGroup = new Group(dto.SubGroupCode);
            await groups.AddAsync(group, ct);
            await groups.AddAsync(subGroup, ct);
        }
        
        var user = await users.GetByTelegramIdAsync(dto.TelegramId, ct);
        if (user is null)
        {
            user = new User(dto.TelegramId, dto.FullName, dto.Username,
                new List<string> { dto.GroupCode, dto.SubGroupCode });
            await users.AddAsync(user, ct);
        }
        else
        {
            user.UpdateInfo(dto.FullName, dto.Username, new List<string> { dto.GroupCode, dto.SubGroupCode });
            await users.UpdateAsync(user, ct);
        }

        await uow.SaveChangesAsync(ct);
        return NoContent();
    }
}