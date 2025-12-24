using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Domain.Entities;

public class Group
{
    [Key]
    public string Code { get; private set; }
    public List<Guid> CategoriesIds { get; private set; } = new();
    public List<Guid> EventsIds { get; private set; } = new();
    
    [Column(TypeName = "long[]")]
    public List<long> UsersTelegramIds { get; private set; }= new();
    
    protected Group() { } 
    public Group(string code)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
    }

    public bool AddCategory(Guid categoryId)
    {
        CategoriesIds.Add(categoryId);
        return true;
    }

    public bool RemoveCategory(Guid categoryId)
    {
        return CategoriesIds.Remove(categoryId);
    }

    public bool AddEvent(Guid eventId)
    {
        EventsIds.Add(eventId);
        return true;
    }

    public bool RemoveEvent(Guid eventId)
    {
        return EventsIds.Remove(eventId);
    }
    
    public bool AddUser(long telegramId)
    {
        UsersTelegramIds.Add(telegramId);
        return true;
    }

    public bool RemoveUser(long telegramId)
    {
        return UsersTelegramIds.Remove(telegramId);
    }
    
    public override bool Equals(object? obj)
        => obj is Group g && g.Code.Equals(Code);

    public override int GetHashCode()
        => Code.GetHashCode();
}
