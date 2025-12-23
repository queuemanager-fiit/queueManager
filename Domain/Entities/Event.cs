using System.ComponentModel.DataAnnotations;
namespace Domain.Entities;

public class Event
{
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();
    public EventCategory Category { get;  set; }
    public List<User> Participants = new();
    public Dictionary<User, UserPreference> Preferences = new();
    public DateTimeOffset OccurredOn { get; set; }
    public DateTimeOffset FormationTime { get; set; }
    public DateTimeOffset DeletionTime { get; set; }
    public DateTimeOffset NotificationTime {get; set;}
    public bool IsNotified { get;  set; }
    public bool IsFormed { get;  set; }
    public string GroupCode { get;set; }

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
        if (Preferences.ContainsKey(user)) return false;

        Preferences[user] = preference;
        Participants.Add(user);
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
        if (!Preferences.ContainsKey(user)) return false;

        int index = Participants.IndexOf(user);
        if (index == -1) return false;

        Preferences.Remove(user);
        Participants.RemoveAt(index);

        return true;
    }

    public bool CanUserJoin(User user) =>
        user is not null && user.GroupCodes.Contains(GroupCode);

    public void FormQueue()
    {
        if (IsFormed) return;

        var unfinished = Category.UnfinishedUsers;

        var sortedParticipants = Preferences
            .OrderBy(p => p.Value)
            .ThenBy(p => p.Key.AveragePosition)
            .Select(p => p.Key)
            .ToList();

        var finalQueue = new List<User>();

        foreach (var user in unfinished)
        {
            if (Preferences.ContainsKey(user) && Preferences[user] != UserPreference.End)
            {
                finalQueue.Add(user);
                sortedParticipants.Remove(user);
            }
        }

        finalQueue.AddRange(sortedParticipants);

        Participants.Clear();
        Participants.AddRange(finalQueue);
        IsFormed = true;

        for (int i = 0; i < Participants.Count; i++)
        {
            var user = Participants[i];
            user.UpdateAveragePosition(i + 1);
        }

        Category.UnfinishedUsers.Clear();
    }

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
    public Guid Id { get; } = Guid.NewGuid();
    public string SubjectName { get; private set;}
    public bool IsAutoCreate { get; private set;}
    public string GroupCode { get; private set;}
    public List<User> UnfinishedUsers { get; private set; }= new();

    public EventCategory(
        string subjectName,
        bool isAutoCreate,
        string groupCode)
    {
        SubjectName = subjectName ?? throw new ArgumentNullException(nameof(subjectName));
        IsAutoCreate = isAutoCreate;
        GroupCode = groupCode ?? throw new ArgumentNullException(nameof(groupCode));
    }

    public void UpdateUnfinishedUsers(IReadOnlyList<User> queue, int position)
    {
        if (queue == null)
            throw new ArgumentNullException(nameof(queue));

        int cutoffPosition = position - 1;

        if (cutoffPosition < 0)
            throw new ArgumentOutOfRangeException(
                nameof(position),
                "Номер позиции должен быть положительным целым числом."
            );

        UnfinishedUsers.Clear();


        for (int i = cutoffPosition; i < queue.Count; i++)
        {
            var user = queue[i];
            if (user != null)
                UnfinishedUsers.Add(user);
        }
    }
    
    public override bool Equals(object? obj)
        => obj is EventCategory e && e.Id == Id;

    public override int GetHashCode()
        => Id.GetHashCode();
}
