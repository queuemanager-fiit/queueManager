namespace Domain.Entities;

public class User
{
    public Guid Id { get; set;} = Guid.NewGuid();
    public Guid GroupId { get; private set; }
    public string FullName { get; private set; }
    public string Username { get; private set; }
    public long TelegramId { get; private set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public UserPreference Preference { get; private set; }
    
    public User() { }
    
    public void SetPreference(UserPreference preference)
    {
        Preference = preference;
    }

    public User(string fullName, string userName, long telegramId, Guid groupId)
    {
        FullName = fullName;
        Username = userName;
        TelegramId = telegramId;
        GroupId = groupId;
    }

    public void UpdateInfo(string newFullName, string newUsername, Guid newGroupId)
    {
        FullName = newFullName;
        Username = newUsername;
        GroupId = newGroupId;
    }
}

public enum UserPreference
{
    Start,
    End,
    NearPerson,
    NoPreference
}