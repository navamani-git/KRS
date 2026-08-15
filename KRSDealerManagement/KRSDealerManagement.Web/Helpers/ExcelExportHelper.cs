using ClosedXML.Excel;

namespace KRSDealerManagement.Web.Helpers
{
    public static class ExcelExportHelper
    {
        public static byte[] Build(string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object?>> rows)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(string.IsNullOrWhiteSpace(sheetName) ? "Export" : sheetName[..Math.Min(sheetName.Length, 31)]);

            for (var c = 0; c < headers.Count; c++)
                ws.Cell(1, c + 1).Value = headers[c];

            var headerRow = ws.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

            var rowIndex = 2;
            foreach (var row in rows)
            {
                for (var c = 0; c < row.Count; c++)
                {
                    var cell = ws.Cell(rowIndex, c + 1);
                    var val = row[c];
                    if (val is DateTime dt) cell.Value = dt;
                    else if (val is decimal dec) cell.Value = dec;
                    else if (val is double dbl) cell.Value = dbl;
                    else if (val is int i) cell.Value = i;
                    else if (val is long l) cell.Value = l;
                    else if (val is bool b) cell.Value = b;
                    else cell.Value = val?.ToString() ?? "";
                }
                rowIndex++;
            }

            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public static Microsoft.AspNetCore.Mvc.FileContentResult ToFileResult(
            Microsoft.AspNetCore.Mvc.Controller controller,
            string fileName,
            IReadOnlyList<string> headers,
            IEnumerable<IReadOnlyList<object?>> rows,
            string? sheetName = null)
        {
            var bytes = Build(sheetName ?? Path.GetFileNameWithoutExtension(fileName), headers, rows);
            return controller.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
