using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Domain.Entities;

public class Event
{
    [Key]
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
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
        Guid category,
        DateTimeOffset occurredOn,
        string groupCode)
    {
        CategoryId = category;
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