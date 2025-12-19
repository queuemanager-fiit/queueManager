namespace Domain.Entities;

public class Event
{
    public Guid Id { get; } = Guid.NewGuid();
    public EventCategory Category { get; private set; }

    private readonly List<User> participants = new();
    private readonly Dictionary<User, UserPreference> preferences = new();

    public IReadOnlyList<User> Participants => participants;
    public IReadOnlyDictionary<User, UserPreference> Preferences => preferences;

    public DateTimeOffset OccurredOn { get; private set; }
    public DateTimeOffset FormationTime { get; private set; }
    public DateTimeOffset DeletionTime { get; private set; }
    public DateTimeOffset NotificationTime {get; private set;}
    public bool IsNotified { get; private set; }
    public bool IsFormed { get; private set; }
    public string GroupCode { get; private set; }

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
        if (preferences.ContainsKey(user)) return false;

        preferences[user] = preference;
        participants.Add(user);
        return true;
    }

    public bool RemoveParticipant(User user)
    {
        if (user == null) return false;
        if (!preferences.ContainsKey(user)) return false;

        int index = participants.IndexOf(user);
        if (index == -1) return false;

        preferences.Remove(user);
        participants.RemoveAt(index);

        return true;
    }

    public bool CanUserJoin(User user) =>
        user is not null && user.GroupCodes.Contains(GroupCode);

    public void FormQueue()
    {
        if (IsFormed) return;

        var unfinished = Category.UnfinishedUsers;

        Category.ClearUnfinishedUsers();

        var sortedParticipants = preferences
            .OrderBy(p => p.Value)
            .ThenBy(p => p.Key.AveragePosition)
            .Select(p => p.Key)
            .ToList();

        var finalQueue = new List<User>();

        foreach (var user in unfinished)
        {
            if (preferences.ContainsKey(user) && preferences[user] != UserPreference.End)
            {
                finalQueue.Add(user);
                sortedParticipants.Remove(user);
            }
        }

        finalQueue.AddRange(sortedParticipants);

        participants.Clear();
        participants.AddRange(finalQueue);
        IsFormed = true;

        for (int i = 0; i < participants.Count; i++)
        {
            var user = participants[i];
            user.UpdateAveragePosition(i + 1);
        }
    }

    public void MarkAsNotified() =>
        IsNotified = true;
}

public class EventCategory
{
    public Guid Id { get; } = Guid.NewGuid();
    public string SubjectName { get; }
    public bool IsAutoCreate { get; }
    public string GroupCode { get; }

    private readonly List<User> unfinishedUsers = new();

    public IReadOnlyList<User> UnfinishedUsers => unfinishedUsers;

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

        unfinishedUsers.Clear();

        for (int i = cutoffPosition; i < queue.Count; i++)
        {
            var user = queue[i];
            if (user != null)
                unfinishedUsers.Add(user);
        }
    }

    public void ClearUnfinishedUsers()
    {
        unfinishedUsers.Clear();
    }
}