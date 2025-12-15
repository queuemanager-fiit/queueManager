namespace Domain.Entities;

public class User
{
    public Guid Id { get; private set;} = Guid.NewGuid();
    public long TelegramId { get; private set;}
    public string FullName { get; private set; }
    public string Username { get; private set; }
    public List<string> GroupCodes { get; private set; }
    public bool IsAdmin { get; private set; }
    public double AveragePosition { get; private set; } = 0.0;
    public int ParticipationCount { get; private set; } = 0;

    public User(long telegramId, string fullName, string username, List<string> groupCodes)
    {
        TelegramId = telegramId;
        FullName = fullName;
        Username = username;
        GroupCodes = groupCodes;
        IsAdmin = false;
    }

    public void UpdateInfo(string fullName, string username, List<string> groupCodes)
    {
        FullName = fullName;
        Username = username;
        GroupCodes = groupCodes;
    }

    public void UpdateAveragePosition(int currentPosition)
    {
        if (currentPosition <= 0)
            throw new ArgumentOutOfRangeException(nameof(currentPosition), "Позиция должна быть положительной.");

        if (ParticipationCount == 0)
        {
            AveragePosition = currentPosition;
        }
        else
        {
            AveragePosition = (AveragePosition * ParticipationCount + currentPosition) / (ParticipationCount + 1);
        }

        ParticipationCount++;
    }

    public void ResetStatistics()
    {
        AveragePosition = 0.0;
        ParticipationCount = 0;
    }

    public void SetAdminStatus(bool isAdmin) => IsAdmin = isAdmin;
    public bool IsAdministrator() => IsAdmin;
}

public enum UserPreference
{
    Start,
    End,
    NoPreference
}