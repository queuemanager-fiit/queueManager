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
    public bool IsFormed { get; private set; } = false;
    public string GroupCode { get; private set; }

    public Event(
        EventCategory category,
        DateTimeOffset occurredOn,
        TimeSpan formationOffset,
        TimeSpan deletionOffset,
        string groupCode)
    {
        Category = category ?? throw new ArgumentNullException(nameof(category));
        OccurredOn = occurredOn;
        FormationTime = occurredOn.Subtract(formationOffset);
        DeletionTime = occurredOn.Add(deletionOffset);
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

    public bool CanUserJoin(User user)
    {
        if (user == null) return false;
        if (user.GroupCode != GroupCode) return false;

        return !Category.IsForSubgroup || user.SubgroupNumber == Category.SubgroupNumber;
    }

    public void FormQueue()
    {
        if (IsFormed) return;

        var sorted = preferences
            .OrderBy(p => p.Value)
            .ThenBy(p => p.Key.AveragePosition)
            .Select(p => p.Key)
            .ToList();

        participants.Clear();
        participants.AddRange(sorted);
        IsFormed = true;
    }
}

public class EventCategory
{
    public Guid Id { get; } = Guid.NewGuid();
    public string SubjectName { get; }
    public bool IsAutoCreate { get; }
    public bool IsForSubgroup { get; }
    public string GroupCode { get; }
    public int? SubgroupNumber { get; }

    private readonly List<User> unfinishedUsers = new();

    public IReadOnlyList<User> UnfinishedUsers => unfinishedUsers;

    public EventCategory(
        string subjectName,
        bool isAutoCreate,
        bool isForSubgroup,
        string groupCode,
        int? subgroupNumber = null)
    {
        SubjectName = subjectName ?? throw new ArgumentNullException(nameof(subjectName));
        IsAutoCreate = isAutoCreate;
        IsForSubgroup = isForSubgroup;
        GroupCode = groupCode ?? throw new ArgumentNullException(nameof(groupCode));

        if (IsForSubgroup)
        {
            SubgroupNumber = subgroupNumber ??
                throw new ArgumentException("Для подгруппы требуется subgroupNumber");
        }
        else
        {
            SubgroupNumber = null;
        }
    }

    public void UpdateUnfinishedUsers(IReadOnlyList<User> queue, int position, bool isIndex = false)
    {
        if (queue == null)
            throw new ArgumentNullException(nameof(queue));

        int cutoffPosition = isIndex ? position : position - 1;

        if (cutoffPosition < 0)
            throw new ArgumentOutOfRangeException(
                nameof(position),
                isIndex
                    ? "Индекс позиции не может быть отрицательным."
                    : "Номер позиции должен быть положительным целым числом."
            );

        unfinishedUsers.Clear();

        for (int i = cutoffPosition; i < queue.Count; i++)
        {
            var user = queue[i];
            if (user != null)
                unfinishedUsers.Add(user);
        }
    }
}