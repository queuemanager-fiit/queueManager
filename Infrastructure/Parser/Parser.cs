using OfficeOpenXml;

namespace Table;

public class Schedule
{
    private ExcelPackage package;
    private readonly TimeParser timeParser;
    private readonly ExcelParser excelParser;
    private readonly string[,] textData;
    private int RowCount => textData.GetLength(0);
    private int ColumnCount => textData.GetLength(1);

    public Schedule(string filePath)
    {
        package = new ExcelPackage(new FileInfo(filePath));
        excelParser = new ExcelParser();
        textData = excelParser.ExtractData(package.Workbook.Worksheets[5]);
        timeParser = new TimeParser(excelParser.ExtractData(package.Workbook.Worksheets[0]));
    }

    public List<GroupInfo> CollectGroupInfo(DateTime dateTime = default)
    {
        if (dateTime == default)
            dateTime = DateTime.Now;
        var groups = new List<GroupInfo>();
        var groupPositionsList = FindGroupPositions().ToList();
        var parity = timeParser.DetermineParity(dateTime) is Parity.Even ? 1 : 0;
        foreach (var position in groupPositionsList)
        {
            var currentGroup = new GroupInfo(textData[position.Item1, position.Item2].Split(",")[0]);
            groups.Add(currentGroup);
            for (var i = position.Item1 + parity; i < RowCount;)
            {
                var text = textData[i, position.Item2];
                var stringDayOfWeek = textData[i, 0];
                if (text != "" && timeParser.DefineDayOfWeek(stringDayOfWeek) is not null)
                {
                    var timeLesson = textData[i, 1].Split()[1].Split(":");
                    var difference = dateTime.DayOfWeek - (DayOfWeek)timeParser.DefineDayOfWeek(stringDayOfWeek)!;
                    var targetDate = dateTime.AddDays(-difference);
                    targetDate = new DateTime(targetDate.Year, targetDate.Month, targetDate.Day,
                        int.Parse(timeLesson[0]), int.Parse(timeLesson[1]), 0);
                    if (!currentGroup.Lessons.ContainsKey(targetDate.DayOfWeek))
                        currentGroup.Lessons[targetDate.DayOfWeek] = new List<Lesson>();
                    var info = textData[i, position.Item2];
                    currentGroup.Lessons[targetDate.DayOfWeek].Add(new Lesson(text.Split(",")[0], info, targetDate));
                }

                i = SkipTwoRows((i, position.Item2));
            }
        }

        groups.ExtractGroupData();
        return groups;
    }

    private int SkipTwoRows((int, int) position)
    {
        var skip = 0;
        while (skip != 2 && RowCount > position.Item1)
        {
            if (textData[position.Item1, 1] != "")
                skip += 1;
            position.Item1 += 1;
        }

        return position.Item1;
    }

    private IEnumerable<(int, int)> FindGroupPositions()
    {
        for (var row = RowCount - 1; row >= 1; row--)
        {
            for (var col = ColumnCount - 1; col >= 0; col--)
            {
                var cell = textData[row, col];
                if (cell.Contains("ФТ-"))
                    yield return (row, col);
            }
        }
    }
}
