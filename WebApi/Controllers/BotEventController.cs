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
        EventCategory Category);
    
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
                    e.Category)));
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
}