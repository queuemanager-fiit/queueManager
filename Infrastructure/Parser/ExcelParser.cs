using OfficeOpenXml;

namespace Table;

internal class ExcelParser
{
    static ExcelParser()
    {
        ExcelPackage.License.SetNonCommercialPersonal("My Name");
    }

    internal string[,] ExtractData(ExcelWorksheet worksheet)
    {
        var startRow = worksheet.Dimension.Start.Row;
        var startColumn = worksheet.Dimension.Start.Column;
        var rowsCount = worksheet.Dimension.End.Row - startRow + 1;
        var columnsCount = worksheet.Dimension.End.Column - startColumn + 1;
        var resultArray = new string[rowsCount, columnsCount];

        Enumerable.Range(0, rowsCount)
            .ToList()
            .ForEach(i => Enumerable.Range(0, columnsCount)
                .ToList()
                .ForEach(j => 
                {
                    var excelRow = startRow + i;
                    var excelCol = startColumn + j;
                    resultArray[i, j] = FindTextCell(worksheet, excelRow, excelCol);
                }));

        return resultArray;
    }
    
    private string FindTextCell(ExcelWorksheet worksheet, int rowIndex, int columnIndex)
    {
        var cell = worksheet.Cells[rowIndex, columnIndex];
        if (cell.Merge)
        {
            var mergedRange = worksheet.MergedCells[cell.Start.Row, cell.Start.Column];
            var topLeftCell = worksheet.Cells[mergedRange.Split(':')[0]];
            return topLeftCell.Text;
        }

        return cell.Text;
    }
}