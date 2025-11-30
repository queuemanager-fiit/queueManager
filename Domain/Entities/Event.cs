namespace Domain.Entities;

public class Event
{
    public Guid Id { get; } = Guid.NewGuid();
    public EventCategory Category { get; private set; }
    public List<User> Users { get; private set; } = new();
    public DateTimeOffset OccurredOn { get; private set; }
    public DateTimeOffset FormationTime { get; private set; }
    public DateTimeOffset DeletionTime { get; private set; }
    public QueueState State { get; private set; }
    public Guid GroupId { get; private set; } 
    
    public Event(
        EventCategory category, 
        DateTimeOffset occurredOn, 
        DateTimeOffset formationTime, 
        DateTimeOffset deletionTime,
        Guid groupId)
    {
        Category = category;
        OccurredOn = occurredOn;
        FormationTime = formationTime;
        DeletionTime = deletionTime;
        State = QueueState.Created;
        GroupId = groupId;
    }

    public bool CanUserJoin(User user)
    {
        return user.GroupId == GroupId;
    }
}

public class EventCategory
{
    public Guid Id { get; } = Guid.NewGuid();
    public string SubjectName { get; private set; }
    public bool IsAutoCreate { get; private set; }
    public List<User> UnfinishedUsers { get; private set; } = new();
    public Guid GroupId { get; private set; }
    
    public EventCategory(string subjectName, bool isAutoCreate, Guid groupId)
    {
        SubjectName = subjectName;
        IsAutoCreate = isAutoCreate;
        GroupId = groupId;
    }

    public void UpdateGroupId(Guid newGroupId)
    {
        GroupId = newGroupId;
    }
}

public enum QueueState
{
    Created,
    Formed,
    Deleted
}
