using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/events")]
public class BotEventController : ControllerBase
{
    private readonly IEventRepository events;
    private readonly IUnitOfWork uow;

    public BotEventController(IEventRepository events, IUnitOfWork uow)
    {
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

    [HttpGet("due")]
    public async Task<ActionResult<List<BotEventDto>>> GetDue(CancellationToken ct)
    {
        var dueEvents = await events.GetDueAsync(DateTimeOffset.UtcNow, ct);

        return Ok(dueEvents
            .Select(e =>
                new BotEventDto(e.Users
                        .Select(u => u.TelegramId)
                        .ToArray(),
                    e.OccurredOn,
                    e.Category,
                    e.Id)));
    }

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
    
    //[HttpPost("confirm")]

    [HttpGet("events-list-for-user")]
    public async Task<ActionResult<List<BotEventDto>>> GetEventsForUser([FromQuery] long telegramId, CancellationToken ct)
    {
        var userEvents = await events.GetForUserAsync(telegramId, ct);
        
        return Ok(userEvents
            .Select(e =>
                new BotEventDto(e.Users
                        .Select(u => u.TelegramId)
                        .ToArray(),
                    e.OccurredOn,
                    e.Category,
                    e.Id)));
    }

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
    
    [HttpPost("add-user")]
    public async Task<IActionResult> AddUser(CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}