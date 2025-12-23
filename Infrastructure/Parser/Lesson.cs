namespace Table;

public class Lesson(string name, DateTime dateTime)
{
    public string Name { get; } = name;
    public DateTime DateTime { get; } = dateTime;
}