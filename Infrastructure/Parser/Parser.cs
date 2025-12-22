using OfficeOpenXml;

namespace Table;

public class Lesson(string name, DateTime dateTime)
{
    public string Name { get; } = name;
    public DateTime DateTime { get; } = dateTime;
}

public class Group(string name)
{
    public string Name { get; } = name;
    public Dictionary<DayOfWeek, List<Lesson>> lessons = new();
}

public enum Parity
{
    Odd,
    Even,
}

public class Schedule(string filePath)
{
    private ExcelPackage package = new(new FileInfo(filePath));

    private readonly Dictionary<string, DayOfWeek> dayDefinder = new()
    {
        {"ПН", DayOfWeek.Monday},
        {"ВТ", DayOfWeek.Tuesday},
        {"СР", DayOfWeek.Wednesday},
        {"ЧТ", DayOfWeek.Thursday},
        {"ПТ", DayOfWeek.Friday},
        {"СБ", DayOfWeek.Saturday},
        {"ВС", DayOfWeek.Sunday + 7},
    };

    static Schedule()
    {
        ExcelPackage.License.SetNonCommercialPersonal("My Name");
    }


    public List<Group> CollectGroupInfo(DateTime dateTime = default)
    {
        if (dateTime == default)
            dateTime = DateTime.Now;
        var groups = new List<Group>();
        var worksheet = package.Workbook.Worksheets[3];
        var groupPositionsList = FindGroupPositions(worksheet).ToList();
        var parity = DetermineParity(dateTime) is Parity.Even ? 1 : 0;
        foreach (var position in groupPositionsList)
        {
            var lessons = new List<Lesson>();
            var currentGroup = new Group(worksheet.Cells[position.Item1, position.Item2].Text.Split(",")[0]);
            groups.Add(currentGroup);
            for (var i = position.Item1 + 1 + parity; i < worksheet.Dimension.End.Row;)
            {
                var text = FindTextCell(worksheet, (i, position.Item2));
                var dayOfWeek = FindTextCell(worksheet, (i, 1));
                if (text != "" && dayDefinder.ContainsKey(dayOfWeek))
                {
                    var timeLesson = FindTextCell(worksheet, (i, 2)).Split()[1].Split(":");
                    var difference = dateTime.DayOfWeek - dayDefinder[FindTextCell(worksheet, (i, 1))];
                    var targetDate = dateTime.AddDays(-difference);
                    targetDate = new DateTime(targetDate.Year, targetDate.Month, targetDate.Day, int.Parse(timeLesson[0]), int.Parse(timeLesson[1]), 0);
                    if (!currentGroup.lessons.ContainsKey(targetDate.DayOfWeek))
                        currentGroup.lessons[targetDate.DayOfWeek] = new List<Lesson>();
                    currentGroup.lessons[targetDate.DayOfWeek].Add(new Lesson(text.Split(",")[0], targetDate));
                    lessons.Add(new Lesson(text.Split(",")[0], new DateTime()));
                }

                i = SkipTwoRows(worksheet, (i, position.Item2));
            }
        }

        return groups;
    }

    private Parity DetermineParity(DateTime dateTime)
    {
        var parity = new Dictionary<Parity, List<(DateTime, DateTime)>>();
        var worksheet = package.Workbook.Worksheets[0];
        for (var i = worksheet.Dimension.Start.Row; i < worksheet.Dimension.End.Row; i++)
        {
            for (var j = worksheet.Dimension.Start.Column; j < worksheet.Dimension.End.Column; j++)
            {
                var text = FindTextCell(worksheet, (i, j));
                var (up, down) = (text.Contains("Верхние"), text.Contains("Нижние"));
                if (up || down)
                {
                    var listParity = new List<(DateTime, DateTime)>();
                    if (up)
                        parity[Parity.Odd] = listParity;

                    else if (down)
                        parity[Parity.Even] = listParity;
                    WriteDates(worksheet, listParity, (i, j));
                    j += 1;
                }
            }
        }

        if (parity[Parity.Even].Any(tupleDate => tupleDate.Item1 <= dateTime && dateTime <= tupleDate.Item2))
            return Parity.Even;
        if (parity[Parity.Odd].Any(tupleDate => tupleDate.Item1 <= dateTime && dateTime <= tupleDate.Item2))
            return Parity.Odd;
        throw new ArgumentException("Date don't match any parity");
    }

    private int SkipTwoRows(ExcelWorksheet worksheet, (int, int) position)
    {
        var skip = 0;
        while (skip != 2 && worksheet.Dimension.Rows >= position.Item1)
        {
            if (FindTextCell(worksheet, (position.Item1, 2)) != "")
                skip += 1;
            position.Item1 += 1;
        }

        return position.Item1;
    }

    private void WriteDates(ExcelWorksheet worksheet, List<(DateTime, DateTime)> listParity, (int, int) startPosition)
    {
        for (var i = startPosition.Item1 + 1; ; i++)
        {
            var leftCellText = FindTextCell(worksheet, (i, startPosition.Item2));
            var rightCellText = FindTextCell(worksheet, (i, startPosition.Item2 + 1));
            if (leftCellText == "")
                break;
            listParity.Add(ParseTimeInterval((leftCellText, rightCellText)));
        }
    }

    private (DateTime, DateTime) ParseTimeInterval((string, string) dates)
    {
        var stringDate = (dates.Item1.Split("."), dates.Item2.Split("."));
        return (
            new DateTime(int.Parse(stringDate.Item1[2]), int.Parse(stringDate.Item1[1]), int.Parse(stringDate.Item1[0]),
                0, 0, 0),
            new DateTime(int.Parse(stringDate.Item2[2]), int.Parse(stringDate.Item2[1]), int.Parse(stringDate.Item2[0]),
                23, 59, 59));
    }

    private string FindTextCell(ExcelWorksheet worksheet, (int, int) position)
    {
        var cell = worksheet.Cells[position.Item1, position.Item2];
        if (cell.Merge)
        {
            var mergedRange = worksheet.MergedCells[cell.Start.Row, cell.Start.Column];
            var topLeftCell = worksheet.Cells[mergedRange.Split(':')[0]];
            return topLeftCell.Text;
        }

        return cell.Text;
    }

    private IEnumerable<(int, int)> FindGroupPositions(ExcelWorksheet worksheet)
    {
        for (var row = worksheet.Dimension.End.Row; row >= worksheet.Dimension.Start.Row + 1; row--)
        {
            for (var col = worksheet.Dimension.End.Column; col >= worksheet.Dimension.Start.Row; col--)
            {
                var cell = worksheet.Cells[row, col];
                if (cell.Text.Contains("ФТ-"))
                    yield return (row, col);
            }
        }
    }
}