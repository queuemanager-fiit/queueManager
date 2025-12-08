using OfficeOpenXml;

namespace Parser.Parser;

public class Lesson(string name, DateTime dateTime)
{
    public string Name { get; } = name;
    public DateTime DateTime { get; } = dateTime;
}

public class GroupInfo(string name, List<Lesson> lessons)
{
    public readonly string name = name;
    public readonly List<Lesson> Lessons = lessons;
}

public enum Parity
{
    Odd,
    Even,
}

public class Schedule
{
    private ExcelPackage package;
    private string lastMessage;



    static Schedule()
    {
        ExcelPackage.License.SetNonCommercialPersonal("My Name");
    }

    public Schedule(string filePath)
    {
        package = new ExcelPackage(new FileInfo(filePath));
        lastMessage = "";
    }


    public List<GroupInfo> CollectGroupInfo(DateTime dateTime = default)
    {
        if (dateTime == default)
            dateTime = DateTime.Now;
        var groups = new List<GroupInfo>();
        var worksheet = package.Workbook.Worksheets[3];
        var groupPositionsList = FindGroupPositions(worksheet).ToList();
        var parity = DetermineParity(DateTime.Now) is Parity.Even ? 1 : 0;
        foreach (var position in groupPositionsList)
        {
            var lessons = new List<Lesson>();
            groups.Add(new GroupInfo(worksheet.Cells[position.Item1, position.Item2].Text.Split(",")[0], lessons));
            for (var i = position.Item1 + 1 + parity; i < worksheet.Dimension.End.Row;)
            {
                var text = FindTextCell(worksheet, (i, position.Item2));
                if (text != "")
                {
                    lessons.Add(new Lesson(text.Split(",")[0], DateTime.Now));
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
            if (leftCellText == "")
                break;
            listParity.Add((ParseDate(leftCellText),
                ParseDate(FindTextCell(worksheet, (i, startPosition.Item2 + 1))))
                );
        }
    }

    private DateTime ParseDate(string date)
    {
        var stringDate = date.Split(".");
        return new DateTime(int.Parse(stringDate[2]), int.Parse(stringDate[1]), int.Parse(stringDate[0]));
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

class Program
{
    static void Main()
    {
        var filePath = @"C:\Users\Егор\Desktop\Table\Расписание.xlsx";
        new Schedule(filePath).CollectGroupInfo();
    }
}