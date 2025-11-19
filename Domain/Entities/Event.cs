namespace Domain.Entities;

public class Event
{
    public Guid Id { get; } = Guid.NewGuid();
    public EventCategory Category { get; private set; }
    public List<User> Users { get; private set; }
    public DateTimeOffset OccurredOn { get; private set; }
}

public enum EventCategory
{
    
}