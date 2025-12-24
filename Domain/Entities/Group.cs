using System.ComponentModel.DataAnnotations;
namespace Domain.Entities;

public class Group
{
    [Key]
    public string Code { get; private set; }
    public List<Guid> CategoriesIds { get; private set; } = new();
    public List<Guid> EventsIds { get; private set; } = new();
    public List<long> UsersTelegramIds { get; private set; }= new();
    
    protected Group() { } 
    public Group(string code)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
    }

    public bool AddCategory(EventCategory category)
    {
        if (category == null) return false;
        CategoriesIds.Add(category);
        return true;
    }

    public bool RemoveCategory(EventCategory category)
    {
        if (category == null) return false;
        return CategoriesIds.Remove(category);
    }

    public bool AddEvent(Event evt)
    {
        if (evt == null) return false;
        EventsIds.Add(evt);
        return true;
    }

    public bool RemoveEvent(Event evt)
    {
        if (evt == null) return false;
        return EventsIds.Remove(evt);
    }
    
    public bool AddUser(User user)
    {
        if (user == null) return false;
        UsersTelegramIds.Add(user);
        return true;
    }

    public bool RemoveUser(User user)
    {
        if (user == null) return false;
        return UsersTelegramIds.Remove(user);
    }
    
    public override bool Equals(object? obj)
        => obj is Group g && g.Code.Equals(Code);

    public override int GetHashCode()
        => Code.GetHashCode();
}
