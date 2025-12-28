namespace Table;

public static class GroupOnly
{
    public static List<GroupInfo> ExtractGroupData(this List<GroupInfo> subGroupInfos)
    {
        var result = new List<GroupInfo>();

        var subGroupDict = subGroupInfos.ToDictionary(g => g.Name);
        var groupToSubGroups = subGroupInfos
            .GroupBy(g => GetBaseGroup(g.Name))
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        foreach (var (groupName, subGroups) in groupToSubGroups)
        {
            var groupInfo = new GroupInfo(groupName);

            var referenceSubGroup = subGroupDict[subGroups[0]];

            foreach (var (day, lessons) in referenceSubGroup.Lessons)
            {
                foreach (var lesson in lessons)
                {
                    if (!IsLessonForExactGroup(
                        lesson.DateTime,
                        lesson.Information,
                        subGroups,
                        subGroupDict))
                        continue;

                    if (!groupInfo.Lessons.ContainsKey(day))
                        groupInfo.Lessons[day] = new List<Lesson>();

                    groupInfo.Lessons[day].Add(
                        new Lesson(lesson.Name, lesson.Information, lesson.DateTime)
                    );
                }
            }
            foreach (var day in groupInfo.Lessons.Keys.ToList())
            {
                groupInfo.Lessons[day] = groupInfo.Lessons[day]
                    .OrderBy(l => l.DateTime)
                    .ToList();
            }

            if (groupInfo.Lessons.Count > 0)
                result.Add(groupInfo);
        }

        return result;
    }

    private static bool IsLessonForExactGroup(
        DateTime dateTime,
        string information,
        List<string> groupSubGroups,
        Dictionary<string, GroupInfo> allSubGroups)
    {
        var foundIn = new HashSet<string>();

        foreach (var (name, info) in allSubGroups)
        {
            if (!info.Lessons.TryGetValue(dateTime.DayOfWeek, out var lessons))
                continue;

            if (lessons.Any(l =>
                l.DateTime == dateTime &&
                l.Information == information))
            {
                foundIn.Add(name);
            }
        }
        return foundIn.SetEquals(groupSubGroups);
    }

    private static string GetBaseGroup(string subGroupName)
    {
        // ФТ-201-1 → ФТ-201
        var parts = subGroupName.Split('-');
        return $"{parts[0]}-{parts[1]}";
    }
}
