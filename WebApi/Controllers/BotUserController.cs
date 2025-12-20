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
        string FullName,
        string Username,
        string GroupCode,
        string SubGroupCode,
        long TelegramId);

    public sealed record InfoUserDto(
        string FullName,
        string Username,
        string GroupCode,
        string SubGroupCode,
        bool IsAdmin,
        double AveragePosition,
        int ParticipationCount);

    public sealed record DeletionUserDto(
        long telegramId,
        string groupCode);

    //вызывается для регистрации пользователя или обновлении информации о нем
    [HttpPost("update-userinfo")]
    public async Task<IActionResult> UpdateUserInfo([FromBody] BotUserDto dto, CancellationToken ct)
    {
        var group = await groups.GetByCodeAsync(dto.GroupCode, ct);
        var subGroup = await groups.GetByCodeAsync(dto.SubGroupCode, ct);
        
        if (group is null)
        {
            group = new Group(dto.GroupCode);
            subGroup = new Group(dto.SubGroupCode);
            await groups.AddAsync(group, ct);
            await groups.AddAsync(subGroup, ct);
        }
        else if (subGroup is null)
        {
            subGroup = new Group(dto.SubGroupCode);
            await groups.AddAsync(subGroup, ct);
        }
        
        var user = await users.GetByTelegramIdAsync(dto.TelegramId, ct);
        if (user is null)
        {
            user = new User(dto.TelegramId, dto.FullName, dto.Username,
                new List<string> { dto.GroupCode, dto.SubGroupCode });
            await users.AddAsync(user, ct);
            group.AddUser(user);
            subGroup.AddUser(user);
        }
        else
        {
            var oldGroup = await groups.GetByCodeAsync(user.GroupCodes.First(), ct);
            var oldSubGroup = await groups.GetByCodeAsync(user.GroupCodes.Last(), ct);
            if (!user.GroupCodes.Contains(dto.GroupCode))
            {
                oldGroup.RemoveUser(user);
                group.AddUser(user);
            }
            
            if (!user.GroupCodes.Contains(dto.SubGroupCode))
            {
                oldSubGroup.RemoveUser(user);
                subGroup.AddUser(user);
            }
            
            user.UpdateInfo(dto.FullName, dto.Username, new List<string> { dto.GroupCode, dto.SubGroupCode });
            await users.UpdateAsync(user, ct);
            await groups.UpdateAsync(oldSubGroup, ct);
            await groups.UpdateAsync(oldGroup, ct);
        }
        
        await groups.UpdateAsync(subGroup, ct);
        await groups.UpdateAsync(group, ct);
        
        await uow.SaveChangesAsync(ct);
        return NoContent();
    }
    
    //вызывается для удаления пользователя из группы
    [HttpPost("delete-user")]
    public async Task<IActionResult> DeleteUserInfo([FromBody] DeletionUserDto dto, CancellationToken ct)
    {
        var user = await users.GetByTelegramIdAsync(dto.telegramId, ct);
        var group = await groups.GetByCodeAsync(dto.groupCode, ct);
        
        user.GroupCodes.Remove(dto.groupCode);
        group.RemoveUser(user);


        await groups.UpdateAsync(group, ct);
        await users.UpdateAsync(user, ct);
        
        await uow.SaveChangesAsync(ct);
        return NoContent();
    }
    
    //возвращает всю информацию о пользователе
    [HttpGet("user-info")]
    public async Task<ActionResult<InfoUserDto>> GetUserInfo([FromQuery] long telegramId, CancellationToken ct)
    {
        var user = await users.GetByTelegramIdAsync(telegramId, ct);
        
        if (user == null)
        {
            return NotFound($"User with TelegramId {telegramId} not found");
        }
    
        return Ok(new InfoUserDto(
            user.FullName,
            user.Username,
            user.GroupCodes.FirstOrDefault() ?? string.Empty,
            user.GroupCodes.LastOrDefault() ?? string.Empty,
            user.IsAdmin,
            user.AveragePosition,
            user.ParticipationCount));
    }
}