using System.ComponentModel.DataAnnotations;
namespace Domain.Entities;

public class Group
{
    [Key]
    public string Code { get; private set;}
    public List<EventCategory> Categories = new();
    public List<Event> Events = new();
    public List<User> Users = new();
    
    protected Group() { } 
    public Group(string code)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
    }

    public bool AddCategory(EventCategory category)
    {
        if (category == null) return false;
        Categories.Add(category);
        return true;
    }

    public bool RemoveCategory(EventCategory category)
    {
        if (category == null) return false;
        return Categories.Remove(category);
    }

    public bool AddEvent(Event evt)
    {
        if (evt == null) return false;
        Events.Add(evt);
        return true;
    }

    public bool RemoveEvent(Event evt)
    {
        if (evt == null) return false;
        return Events.Remove(evt);
    }
    
    public bool AddUser(User user)
    {
        if (user == null) return false;
        Users.Add(user);
        return true;
    }

    public bool RemoveUser(User user)
    {
        if (user == null) return false;
        return Users.Remove(user);
    }
}
