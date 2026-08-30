using System.Globalization;
using ClosedXML.Excel;
using KRSDealerManagement.Web.Models;

namespace KRSDealerManagement.Web.Helpers.ExcelImport
{
    public static class ExcelImportWorkbookHelper
    {
        public const string DataSheetName = "Data";
        public const string LookupsSheetName = "Lookups";
        public const string RowTypeColumn = "RowType";

        public static byte[] BuildTemplate(
            string dataSheetName,
            IReadOnlyList<string> dataHeaders,
            IReadOnlyList<IReadOnlyList<object?>> exampleRows,
            IReadOnlyDictionary<string, IReadOnlyList<string>> lookups,
            IReadOnlyList<ExcelReferenceTable>? referenceTables = null)
        {
            using var workbook = new XLWorkbook();
            var sheetName = dataSheetName.Length > 31 ? dataSheetName[..31] : dataSheetName;
            var ws = workbook.Worksheets.Add(sheetName);

            var allHeaders = dataHeaders.Concat(new[] { RowTypeColumn }).ToList();
            for (var c = 0; c < allHeaders.Count; c++)
                ws.Cell(1, c + 1).Value = allHeaders[c];

            var headerRow = ws.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

            var rowIndex = 2;
            foreach (var example in exampleRows)
            {
                WriteDataRow(ws, rowIndex++, dataHeaders, example, isExample: true);
            }

            ws.Cell(rowIndex, 1).Value = "Add your rows below. Leave RowType blank on your rows (only the sample row uses EXAMPLE and is skipped).";
            ws.Range(rowIndex, 1, rowIndex, allHeaders.Count).Merge();
            ws.Row(rowIndex).Style.Font.Italic = true;
            ws.Row(rowIndex).Style.Font.FontColor = XLColor.Gray;

            ws.Columns().AdjustToContents();

            var lookupWs = workbook.Worksheets.Add(LookupsSheetName);
            lookupWs.Cell(1, 1).Value = "Lookup";
            lookupWs.Cell(1, 2).Value = "Valid Values";
            lookupWs.Row(1).Style.Font.Bold = true;

            var lookupRow = 2;
            foreach (var kv in lookups)
            {
                lookupWs.Cell(lookupRow, 1).Value = kv.Key;
                lookupWs.Cell(lookupRow, 2).Value = string.Join(", ", kv.Value);
                lookupRow++;
            }

            if (referenceTables is { Count: > 0 })
            {
                lookupRow += 1;
                foreach (var table in referenceTables)
                {
                    lookupWs.Cell(lookupRow, 1).Value = table.Title;
                    lookupWs.Row(lookupRow).Style.Font.Bold = true;
                    lookupRow++;

                    for (var c = 0; c < table.Headers.Count; c++)
                    {
                        lookupWs.Cell(lookupRow, c + 1).Value = table.Headers[c];
                        lookupWs.Cell(lookupRow, c + 1).Style.Font.Bold = true;
                    }
                    lookupRow++;

                    foreach (var row in table.Rows)
                    {
                        for (var c = 0; c < table.Headers.Count; c++)
                        {
                            var cell = lookupWs.Cell(lookupRow, c + 1);
                            if (c < row.Count)
                            {
                                var val = row[c];
                                if (val is DateTime dt) cell.Value = dt;
                                else if (val is decimal dec) cell.Value = dec;
                                else if (val is double dbl) cell.Value = dbl;
                                else if (val is int i) cell.Value = i;
                                else cell.Value = val?.ToString() ?? "";
                            }
                        }
                        lookupRow++;
                    }

                    lookupRow += 1;
                }
            }

            lookupWs.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public static List<ExcelImportRow> ParseDataRows(Stream stream)
        {
            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheets.FirstOrDefault(w =>
                w.Name.Equals(DataSheetName, StringComparison.OrdinalIgnoreCase))
                ?? workbook.Worksheets.First();

            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
            if (lastCol == 0) return new List<ExcelImportRow>();

            var headers = new List<string>();
            for (var c = 1; c <= lastCol; c++)
            {
                var h = ws.Cell(1, c).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(h))
                    headers.Add(h);
            }

            if (headers.Count == 0)
                throw new InvalidOperationException("The Data sheet has no header row.");

            var rows = new List<ExcelImportRow>();
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            for (var r = 2; r <= lastRow; r++)
            {
                var firstCell = ws.Cell(r, 1).GetString().Trim();
                if (firstCell.StartsWith("Add your rows below", StringComparison.OrdinalIgnoreCase))
                    continue;

                var cells = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var anyValue = false;
                for (var c = 0; c < headers.Count; c++)
                {
                    if (headers[c].Equals(RowTypeColumn, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var val = GetCellValue(ws.Cell(r, c + 1));
                    if (!string.IsNullOrWhiteSpace(val)) anyValue = true;
                    cells[headers[c]] = val;
                }

                if (!anyValue) continue;

                var rowType = headers.Contains(RowTypeColumn, StringComparer.OrdinalIgnoreCase)
                    ? ws.Cell(r, headers.FindIndex(h => h.Equals(RowTypeColumn, StringComparison.OrdinalIgnoreCase)) + 1).GetString().Trim()
                    : null;
                if (string.Equals(rowType, "EXAMPLE", StringComparison.OrdinalIgnoreCase))
                    continue;

                rows.Add(new ExcelImportRow { RowNumber = r, Cells = cells });
            }

            return rows;
        }

        private static void WriteDataRow(IXLWorksheet ws, int rowIndex, IReadOnlyList<string> dataHeaders, IReadOnlyList<object?> values, bool isExample)
        {
            for (var c = 0; c < dataHeaders.Count; c++)
            {
                var cell = ws.Cell(rowIndex, c + 1);
                if (c < values.Count)
                {
                    var val = values[c];
                    if (val is DateTime dt) cell.Value = dt;
                    else if (val is decimal dec) cell.Value = dec;
                    else if (val is double dbl) cell.Value = dbl;
                    else if (val is int i) cell.Value = i;
                    else cell.Value = val?.ToString() ?? "";
                }
            }

            ws.Cell(rowIndex, dataHeaders.Count + 1).Value = isExample ? "EXAMPLE" : "";
        }

        internal static string GetCellValue(IXLCell cell)
        {
            if (cell.IsEmpty())
                return string.Empty;

            return cell.DataType switch
            {
                XLDataType.DateTime => cell.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                XLDataType.Number => FormatNumber(cell.GetDouble()),
                XLDataType.Boolean => cell.GetBoolean() ? "Yes" : "No",
                _ => cell.GetString().Trim()
            };
        }

        private static string FormatNumber(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return string.Empty;

            if (Math.Abs(value - Math.Round(value)) < 0.0000001)
                return ((long)Math.Round(value)).ToString(CultureInfo.InvariantCulture);

            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
