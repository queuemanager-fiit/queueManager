namespace Domain.Entities;

public class Group
{
    public string Code { get; }
    private readonly List<EventCategory> categories = new();
    private readonly List<Event> events = new();
    private readonly List<User> users = new();
    public IReadOnlyList<Event> GetEvents() => events;
    
    public IReadOnlyList<EventCategory> GetCategories() => categories;
    public IReadOnlyList<User> GetUsers() => users;
    
    public Group(string code)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
    }

    public bool AddCategory(EventCategory category)
    {
        if (category == null) return false;
        categories.Add(category);
        return true;
    }

    public bool RemoveCategory(EventCategory category)
    {
        if (category == null) return false;
        return categories.Remove(category);
    }

    public bool AddEvent(Event evt)
    {
        if (evt == null) return false;
        events.Add(evt);
        return true;
    }

    public bool RemoveEvent(Event evt)
    {
        if (evt == null) return false;
        return events.Remove(evt);
    }
    
    public bool AddUser(User user)
    {
        if (user == null) return false;
        users.Add(user);
        return true;
    }

    public bool RemoveUser(User user)
    {
        if (user == null) return false;
        return users.Remove(user);
    }
}