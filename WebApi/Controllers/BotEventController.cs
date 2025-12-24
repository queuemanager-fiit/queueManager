using Domain.Entities;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/events")]
public class BotEventController : ControllerBase
{
    private readonly IUserRepository users;
    private readonly IEventRepository events;
    private readonly IEventCategoryRepository eventCategories;
    private readonly IGroupRepository groups;
    private readonly IUnitOfWork uow;

    public BotEventController(IUserRepository users,
        IEventRepository events,
        IEventCategoryRepository eventCategories,
        IGroupRepository groups,
        IUnitOfWork uow)
    {
        this.users = users;
        this.events = events;
        this.eventCategories = eventCategories;
        this.groups = groups;
        this.uow = uow;
    }

    public sealed record BotEventDto(
        long[] TelegramId,
        DateTimeOffset OccurredOn,
        string Category,
        Guid EventId,
        string GroupCode);

    public sealed record ParticipationDto(
        long TelegramId,
        Guid EventId,
        UserPreference UserPreference);
    
    public sealed record CancellationDto(
        long TelegramId,
        Guid EventId);
    
    public sealed record CreationDto(
        string GroupCode,
        string CategoryName,
        DateTimeOffset OccurredOn
        );

    public sealed record DeletionDto(
        string GroupCode,
        Guid EventId);
    

    public sealed record MarkNotifiedEvents(List<Guid> Ids);
    
    public sealed record MarkUnfinishedUsers(Guid EventId, int FirstUnfinishedPosition);

    private async Task<List<BotEventDto>> ToDtoList(List<Event> list, CancellationToken ct)
    {
        var dtoList = new List<BotEventDto>();


        foreach (var e in list)
        {
            var category = await eventCategories.GetByIdAsync(e.CategoryId, ct);
            dtoList.Add(new BotEventDto(
                e.ParticipantsTelegramIds.ToArray(),
                e.OccurredOn,
                category?.SubjectName ?? "Unknown",
                e.Id,
                e.GroupCode));
        }

        return dtoList;
    }

    //вызывается раз в определенный срок, чтобы узнать, есть ли события, по которым пора выслать уведомление
    [HttpGet("due-events-notification")]
    public async Task<ActionResult<List<BotEventDto>>> GetDueNotification(CancellationToken ct) =>
        Ok(await ToDtoList(await events.GetDueNotificationAsync(DateTimeOffset.UtcNow, ct), ct));

    //вызывается раз в определенный срок, чтобы узнать, есть ли события, по которым сформировалась очередь
    [HttpGet("due-events-formation")]
    public async Task<ActionResult<List<BotEventDto>>> GetDueFormation(CancellationToken ct) =>
        Ok(await ToDtoList(await events.GetDueFormationAsync(DateTimeOffset.UtcNow, ct), ct));

    //используется, чтобы отметить события, по которым были высланы уведомления
    [HttpPost("mark-notified")]
    public async Task<IActionResult> MarkNotified(
        [FromBody] MarkNotifiedEvents request,
        CancellationToken ct)
    {
        if (request.Ids is null || request.Ids.Count == 0)
            return BadRequest("No ids provided");

        foreach (var id in request.Ids)
        {
            var ev =  await events.GetByIdAsync(id, ct);
            ev.MarkAsNotified();
            await events.UpdateAsync(ev, ct);
        }
        
        await uow.SaveChangesAsync(ct);

        return NoContent();
    }


    //используется, чтобы отметить, что данный студент желает участвовать в данной очереди
    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] ParticipationDto dto, CancellationToken ct)
    {
        var user = await users.GetByTelegramIdAsync(dto.TelegramId, ct);
        var ev = await events.GetByIdAsync(dto.EventId, ct);
        
        ev.AddParticipant(user, dto.UserPreference);
        
        await events.UpdateAsync(ev, ct);
        await uow.SaveChangesAsync(ct);
        
        return NoContent();
    }
    
    //используется, чтобы отменить участие в очереди для пользователя
    [HttpPost("quit-queue")]
    public async Task<IActionResult> QuitQueue([FromBody] CancellationDto dto, CancellationToken ct)
    {
        var ev =  await events.GetByIdAsync(dto.EventId, ct);
        var user = await users.GetByTelegramIdAsync(dto.TelegramId, ct);
        ev.RemoveParticipant(user);
        
        await events.UpdateAsync(ev, ct);
        await uow.SaveChangesAsync(ct);
        
        return NoContent();
    }
    
    //используется, чтобы создать очередь. возвращает айди очереди
    [HttpPost("create-queue")]
    public async Task<ActionResult<Guid>> CreateQueue([FromBody] CreationDto dto, CancellationToken ct)
    {
        var group = await groups.GetByCodeAsync(dto.GroupCode, ct);
        var category = await eventCategories.GetByGroupIdAndNameAsync(
            dto.GroupCode,
            dto.CategoryName,
            ct);
        
        var ev = new Event(
            category.Id,
            dto.OccurredOn,
            dto.GroupCode);
        await events.AddAsync(ev, ct);
        
        group.AddEvent(ev.Id);
        await groups.UpdateAsync(group, ct);
        
        await uow.SaveChangesAsync(ct);
        return Ok(ev.Id);
    }
    
    //используется, чтобы удалить очередь
    [HttpPost("delete-queue")]
    public async Task<IActionResult> DeleteQueue([FromBody] DeletionDto dto, CancellationToken ct)
    {
        var group = await groups.GetByCodeAsync(dto.GroupCode, ct);
        var ev = await events.GetByIdAsync(dto.EventId, ct);
        group.RemoveEvent(ev.Id);
        await groups.UpdateAsync(group, ct);
        await events.DeleteAsync(ev, ct);
        
        await uow.SaveChangesAsync(ct);
        return NoContent();
    }

    //возвращает все активные очереди для группы
    [HttpGet("events-for-group")]
    public async Task<ActionResult<List<BotEventDto>>> GetForGroup([FromQuery] string groupCode, CancellationToken ct)
    {
        var group = await groups.GetByCodeAsync(groupCode, ct);
        if (group == null)
            return NotFound($"Group with Group Code {groupCode} not found");

        var tasks = group.EventsIds.Select(id => events.GetByIdAsync(id, ct));
        var eventsList = await Task.WhenAll(tasks);
        return Ok(await ToDtoList(eventsList.ToList(), ct));
    }

    //используется, чтобы отметить неуспевших пользователей
    [HttpPost("mark-unfinished")]
    public async Task<IActionResult> MarkUnfinished([FromBody] MarkUnfinishedUsers request, CancellationToken ct)
    {
        var ev = await events.GetByIdAsync(request.EventId, ct);
        var category = await eventCategories.GetByIdAsync(ev.CategoryId, ct);

        if (category == null)
            return NotFound($"Category with Id {ev.CategoryId} not found");

        category.UpdateUnfinishedUsers(ev.ParticipantsTelegramIds, request.FirstUnfinishedPosition);
        await eventCategories.UpdateAsync(category, ct);
        await uow.SaveChangesAsync(ct);

        return NoContent();
    }

    //возвращает информацию об очередях, в которых участвует пользователь
    [HttpGet("user-info-events")]
    public async Task<ActionResult<List<BotEventDto>>> GetUserEventsInfo([FromQuery] long telegramId, CancellationToken ct)
    {
        var user = await users.GetByTelegramIdAsync(telegramId, ct);
        
        if (user == null)
        {
            return NotFound($"User with TelegramId {telegramId} not found");
        }
        
        var group = await groups.GetByCodeAsync(user.GroupCodes.First(), ct);
        var subGroup = await groups.GetByCodeAsync(user.GroupCodes.Last(), ct);
        var eventsIds = group.EventsIds.Concat(subGroup.EventsIds).ToList();
        var checks = await Task.WhenAll(
            eventsIds.Select(async id =>
            {
                var ev = await events.GetByIdAsync(id, ct);
                return (ev, ok: ev != null && ev.ParticipantsTelegramIds.Contains(user.TelegramId));
            })
        );
        
        var filteredIds = checks
            .Where(x => x.ok)
            .Select(x => x.ev)
            .ToList();

        return Ok(await ToDtoList(filteredIds, ct));
    }
}
