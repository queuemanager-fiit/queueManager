using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Domain.Entities;

public class Event
{
    [Key]
    public Guid Id { get; private set; } = Guid.NewGuid();
    public EventCategory Category { get; set; }
    public List<long> ParticipantsTelegramIds { get; private set; } = new();
    public List<UserPreference> Preferences { get; private set; } = new();
    public DateTimeOffset OccurredOn { get; set; }
    public DateTimeOffset FormationTime { get; set; }
    public DateTimeOffset DeletionTime { get; set; }
    public DateTimeOffset NotificationTime { get; set; }
    public bool IsNotified { get; set; }
    public bool IsFormed { get; set; }
    public string GroupCode { get; set; }

    protected Event() { }

    public Event(
        EventCategory category,
        DateTimeOffset occurredOn,
        string groupCode)
    {
        Category = category ?? throw new ArgumentNullException(nameof(category));
        OccurredOn = occurredOn;
        FormationTime = occurredOn.Subtract(TimeSpan.FromDays(1));
        NotificationTime = FormationTime.Subtract(TimeSpan.FromDays(1));
        DeletionTime = occurredOn.Add(TimeSpan.FromDays(1));
        GroupCode = groupCode ?? throw new ArgumentNullException(nameof(groupCode));
    }

    public bool AddParticipant(User user, UserPreference preference)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        if (!CanUserJoin(user)) return false;

        int index = ParticipantsTelegramIds.IndexOf(user.TelegramId);
        if (index != -1) return false;

        ParticipantsTelegramIds.Add(user.TelegramId);
        Preferences.Add(preference);
        return true;
    }

    public void MarkAsNotified(DateTimeOffset time)
    {
        IsNotified = true;
        NotificationTime = time;
    }

    public void MarkAsFormed(DateTimeOffset time)
    {
        IsFormed = true;
        FormationTime = time;
    }

    public bool RemoveParticipant(User user)
    {
        if (user == null) return false;

        int index = ParticipantsTelegramIds.IndexOf(user.TelegramId);
        if (index == -1) return false;

        ParticipantsTelegramIds.RemoveAt(index);
        Preferences.RemoveAt(index);

        return true;
    }

    public bool CanUserJoin(User user) =>
        user is not null && user.GroupCodes.Contains(GroupCode);

    public void MarkAsNotified() =>
        IsNotified = true;

    public override bool Equals(object? obj)
        => obj is Event e && e.Id == Id;

    public override int GetHashCode()
        => Id.GetHashCode();
}

public class EventCategory
{
    [Key]
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string SubjectName { get; private set;}
    public bool IsAutoCreate { get; private set;}
    public string GroupCode { get; private set;}
    public List<long> UnfinishedUsersTelegramIds { get; private set; }= new();

    public EventCategory(
        string subjectName,
        bool isAutoCreate,
        string groupCode)
    {
        SubjectName = subjectName ?? throw new ArgumentNullException(nameof(subjectName));
        IsAutoCreate = isAutoCreate;
        GroupCode = groupCode ?? throw new ArgumentNullException(nameof(groupCode));
    }

    public void UpdateUnfinishedUsers(IReadOnlyList<long> queue, int position)
    {
        if (queue == null)
            throw new ArgumentNullException(nameof(queue));

        int cutoffPosition = position - 1;

        if (cutoffPosition < 0)
            throw new ArgumentOutOfRangeException(
                nameof(position),
                "Номер позиции должен быть положительным целым числом."
            );

        UnfinishedUsersTelegramIds.Clear();


        for (int i = cutoffPosition; i < queue.Count; i++)
        {
            var user = queue[i];
            UnfinishedUsersTelegramIds.Add(user);
        }
    }
    
    public override bool Equals(object? obj)
        => obj is EventCategory e && e.Id == Id;

    public override int GetHashCode()
        => Id.GetHashCode();
}
