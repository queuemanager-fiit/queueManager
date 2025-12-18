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


/*public class Group : GroupBase
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Code { get; }
    private readonly List<Subgroup> subgroups = new();
    public IReadOnlyList<Subgroup> Subgroups => subgroups;

    public Group(string code)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
    }

    public IEnumerable<User> GetAllUsers() =>
        subgroups
            .SelectMany(subgroup => subgroup.Users)
            .Distinct();

    public bool AddSubgroup(Subgroup subgroup)
    {
        if (subgroup == null) return false;
        subgroups.Add(subgroup);
        return true;
    }
}
public class Subgroup : GroupBase
{
    public Guid Id { get; } = Guid.NewGuid();
    public string GroupCode { get; }
    public int SubgroupNumber { get; }
    private readonly List<User> users = new();
    public IReadOnlyList<User> Users => users;

    public Subgroup(string groupCode, int subgroupNumber)
    {
        GroupCode = groupCode ?? throw new ArgumentNullException(nameof(groupCode));
        SubgroupNumber = subgroupNumber;
    }

    public bool AddUser(User user)
    {
        if (user == null) return false;
        if (!users.Contains(user))
        {
            users.Add(user);
            return true;
        }
        return false;
    }

    public bool RemoveUser(User user) => users.Remove(user);
}*/