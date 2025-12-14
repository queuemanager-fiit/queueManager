using Application.Interfaces;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class EventSchedulerService : IEventSchedulerService
{
    private readonly IEventCategoryRepository _categoryRepo;
    private readonly IEventRepository _eventRepo;
    private readonly IGroupRepository _groupRepo;
    private readonly IUnitOfWork _uow;

    public EventSchedulerService(
        IEventCategoryRepository categoryRepo,
        IEventRepository eventRepo,
        IGroupRepository groupRepo,
        IUnitOfWork uow)
    {
        _categoryRepo = categoryRepo;
        _eventRepo = eventRepo;
        _groupRepo = groupRepo;
        _uow = uow;
    }

    public async Task CreateEventsFromScheduleAsync(
        IEnumerable<ScheduleEntry> scheduleEntries,
        CancellationToken ct)
    {
        foreach (var entry in scheduleEntries)
        {
            var group = await _groupRepo.GetByCodeAsync(entry.GroupCode, ct);
            if (group == null) continue;

            var category = await _categoryRepo.GetByGroupIdAndNameAsync(
                group.Id,
                entry.SubjectName,
                ct);

            if (category == null || !category.IsAutoCreate)
                continue;

            if (entry.SubgroupNumber.HasValue)
            {
                var subgroup = group.Subgroups
                    .FirstOrDefault(s => s.SubgroupNumber == entry.SubgroupNumber);
                if (subgroup == null)
                    continue;
            }

            var evt = new Event(
                category: category,
                occurredOn: entry.OccurredOn,
                formationOffset: TimeSpan.FromHours(12),
                deletionOffset: TimeSpan.FromHours(12),
                groupCode: entry.GroupCode
            );

            await _eventRepo.AddAsync(evt, ct);
        }

        await _uow.SaveChangesAsync(ct);
    }

    public async Task FormQueuesForDueEventsAsync(
        DateTimeOffset now,
        CancellationToken ct)
    {
        var dueEvents = await _eventRepo.GetDueAsync(now, ct);

        foreach (var evt in dueEvents)
        {
            evt.FormQueue();
            await _eventRepo.UpdateAsync(evt, ct);
        }

        await _uow.SaveChangesAsync(ct);
    }
}
