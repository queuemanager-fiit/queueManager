namespace Table;

public class GroupInfo(string name)
{
    public string Name { get; } = name;
    public Dictionary<DayOfWeek, List<Lesson>> Lessons = new ();
}