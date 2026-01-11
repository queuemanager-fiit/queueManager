using OfficeOpenXml;

namespace Table;

internal class TimeParser(string[,] textData)
{
    
    private readonly Dictionary<string, DayOfWeek> dayDefining = new ()
    {
        {"ПН", DayOfWeek.Monday},
        {"ВТ", DayOfWeek.Tuesday},
        {"СР", DayOfWeek.Wednesday},
        {"ЧТ", DayOfWeek.Thursday},
        {"ПТ", DayOfWeek.Friday},
        {"СБ", DayOfWeek.Saturday},
        {"ВС", DayOfWeek.Sunday + 7},
    };
    
    internal Parity DetermineParity(DateTime dateTime)
    {
        var parity = new Dictionary<Parity, List<(DateTime, DateTime)>>();
        for (var i = 0; i < textData.GetLength(0); i++)
        {
            for (var j = 0; j < textData.GetLength(0); j++)
            {
                var text = textData[i, j];
                var (up, down) = (text.Contains("Верхние"), text.Contains("Нижние"));
                if (up || down)
                {
                    var listParity = new List<(DateTime, DateTime)>();
                    if (up)
                        parity[Parity.Odd] = listParity;

                    else if (down)
                        parity[Parity.Even] = listParity;
                    WriteDates(textData, listParity, (i, j));
                    j += 1;
                }
            }
        }

        if (parity[Parity.Even].Any(tupleDate => tupleDate.Item1 <= dateTime && dateTime <= tupleDate.Item2))
            return Parity.Even;
        return Parity.Odd;
    }
    
    internal DayOfWeek? DefineDayOfWeek(string stringDate)
    {
        if (stringDate is null || !dayDefining.ContainsKey(stringDate))
            return null;

        return dayDefining[stringDate];
    }
        
    
    private void WriteDates(string[,] textData, List<(DateTime, DateTime)> listParity, (int, int) startPosition)
    {
        for (var i = startPosition.Item1 + 1;; i++)
        {
            var leftCellText = textData[i, startPosition.Item2];
            var rightCellText = textData[i, startPosition.Item2 + 1];
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
}