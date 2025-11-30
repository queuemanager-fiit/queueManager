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
    private readonly IUnitOfWork uow;

    public BotEventController(IUserRepository users, IEventRepository events, IUnitOfWork uow)
    {
        this.users = users;
        this.events = events;
        this.uow = uow;
    }

    public sealed record BotEventDto(
        long[] TelegramId,
        DateTimeOffset OccurredOn,
        EventCategory Category,
        Guid EventId);

    public sealed record ParticipationDto(
        long TelegramId,
        Guid EventId);

    public sealed record MarkNotifiedEvents(List<Guid> Ids);

    private List<BotEventDto> ToDtoList(List<Event> list)
    {
        return list
            .Select(e =>
                new BotEventDto(e.Participants
                        .Select(u => u.TelegramId)
                        .ToArray(),
                    e.OccurredOn,
                    e.Category,
                    e.Id))
            .ToList();
    }

    [HttpGet("due-events")]
    public async Task<ActionResult<List<BotEventDto>>> GetDue(CancellationToken ct) =>
        Ok(ToDtoList(await events.GetDueAsync(DateTimeOffset.UtcNow, ct)));

    [HttpPost("mark-notified")]
    public async Task<IActionResult> MarkNotified(
        [FromBody] MarkNotifiedEvents request,
        CancellationToken ct)
    {
        if (request.Ids is null || request.Ids.Count == 0)
            return BadRequest("No ids provided");

        await events.MarkAsNotifiedAsync(request.Ids,
            DateTimeOffset.UtcNow,
            ct);
        await uow.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] ParticipationDto dto, CancellationToken ct)
    {
        var user = await users.GetByTelegramIdAsync(dto.TelegramId, ct);
        var ev = await events.GetByIdAsync(dto.EventId, ct);
        
        ev.AddParticipant(user);
        
        await events.UpdateAsync(ev, ct);
        await uow.SaveChangesAsync(ct);
        
        return NoContent();
    }

    [HttpGet("events-list-for-user")]
    public async Task<ActionResult<List<BotEventDto>>> GetEventsForUser(
        [FromQuery] long telegramId,
        CancellationToken ct) =>
        Ok(ToDtoList(await events.GetForUserAsync(telegramId, ct)));

    [HttpGet("events-list-created-by")]
    public async Task<ActionResult<List<BotEventDto>>> GetEventsCreatedBy(
        [FromQuery] long telegramId,
        CancellationToken ct)
    {
        throw new NotImplementedException();
    }
    
    [HttpGet("category-list")]
    public async Task<ActionResult<List<BotEventDto>>> GetCategories(CancellationToken ct)
    {
        throw new NotImplementedException();
    }
    
    [HttpPost("add-category")]
    public async Task<IActionResult> AddCategory(CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}