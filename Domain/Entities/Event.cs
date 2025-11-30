namespace Domain.Entities;

public class Event
{
    public Guid Id { get; } = Guid.NewGuid();
    public EventCategory Category { get; private set; }
    public List<User> Participants { get; private set; }
    public DateTimeOffset OccurredOn { get; private set; }

    public void AddParticipant(User user) => Participants.Add(user);
}

public enum EventCategory
{
    
}