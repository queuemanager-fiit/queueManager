namespace Table;

public class Lesson(string name, string information, DateTime dateTime)
{
    public string Name { get; } = name;
    internal string Information = information;
    public DateTime DateTime { get; } = dateTime;
}