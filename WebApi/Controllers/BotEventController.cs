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
    private readonly IUnitOfWork uow;

    public BotEventController(IUserRepository users, IEventRepository events, IEventCategoryRepository eventCategories, IUnitOfWork uow)
    {
        this.users = users;
        this.events = events;
        this.eventCategories = eventCategories;
        this.uow = uow;
    }

    public sealed record BotEventDto(
        long[] TelegramId,
        DateTimeOffset OccurredOn,
        string Category,
        Guid EventId);

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
    

    public sealed record MarkNotifiedEvents(List<Guid> Ids);

    private List<BotEventDto> ToDtoList(List<Event> list)
    {
        return list
            .Select(e =>
                new BotEventDto(e.Participants
                        .Select(u => u.TelegramId)
                        .ToArray(),
                    e.OccurredOn,
                    e.Category.SubjectName,
                    e.Id))
            .ToList();
    }

    //вызывается раз в определенный срок, чтобы узнать, есть ли события, по которым пора выслать уведомление
    [HttpGet("due-events")]
    public async Task<ActionResult<List<BotEventDto>>> GetDue(CancellationToken ct) =>
        Ok(ToDtoList(await events.GetDueAsync(DateTimeOffset.UtcNow, ct)));

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
        var category = await eventCategories.GetByGroupIdAndNameAsync(
            dto.GroupCode,
            dto.CategoryName,
            ct);
        var ev = new Event(
            category,
            dto.OccurredOn,
            dto.GroupCode);
        await events.AddAsync(ev, ct);
        
        await uow.SaveChangesAsync(ct);
        return Ok(ev.Id);
    }
    
    //используется, чтобы удалить очередь
    [HttpPost("delete-queue")]
    public async Task<IActionResult> DeleteQueue([FromQuery] Guid eventId, CancellationToken ct)
    {
        var ev = await events.GetByIdAsync(eventId, ct);
        await events.DeleteAsync(ev, ct);
        
        await uow.SaveChangesAsync(ct);
        return NoContent();
    }
}