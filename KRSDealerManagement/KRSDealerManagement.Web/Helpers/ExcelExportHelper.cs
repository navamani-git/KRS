using System.Globalization;
using ClosedXML.Excel;

namespace KRSDealerManagement.Web.Helpers
{
    public static class ExcelExportHelper
    {
        /// <summary>Indian-style grouping with thousands separators (e.g. 12,34,567.89).</summary>
        private const string AmountNumberFormat = "#,##,##0.00";

        public static byte[] Build(string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object?>> rows)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(string.IsNullOrWhiteSpace(sheetName) ? "Export" : sheetName[..Math.Min(sheetName.Length, 31)]);

            var amountColumns = headers
                .Select((header, index) => (IsAmountColumn(header), index))
                .Where(x => x.Item1)
                .Select(x => x.index)
                .ToHashSet();

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
                    SetCellValue(cell, row[c], amountColumns.Contains(c));
                }
                rowIndex++;
            }

            foreach (var colIndex in amountColumns)
            {
                var columnNumber = colIndex + 1;
                if (rowIndex > 2)
                    ws.Range(2, columnNumber, rowIndex - 1, columnNumber).Style.NumberFormat.Format = AmountNumberFormat;
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

        private static void SetCellValue(IXLCell cell, object? val, bool isAmountColumn)
        {
            if (val is DateTime dt)
            {
                cell.Value = dt;
                return;
            }

            if (val is bool b)
            {
                cell.Value = b;
                return;
            }

            if (isAmountColumn && TryCoerceDecimal(val, out var amount))
            {
                cell.Value = amount;
                cell.Style.NumberFormat.Format = AmountNumberFormat;
                return;
            }

            if (val is decimal dec)
            {
                cell.Value = dec;
                if (isAmountColumn) cell.Style.NumberFormat.Format = AmountNumberFormat;
                return;
            }

            if (val is double dbl)
            {
                cell.Value = dbl;
                if (isAmountColumn) cell.Style.NumberFormat.Format = AmountNumberFormat;
                return;
            }

            if (val is int i)
            {
                cell.Value = i;
                if (isAmountColumn) cell.Style.NumberFormat.Format = AmountNumberFormat;
                return;
            }

            if (val is long l)
            {
                cell.Value = l;
                if (isAmountColumn) cell.Style.NumberFormat.Format = AmountNumberFormat;
                return;
            }

            cell.Value = val?.ToString() ?? "";
        }

        private static bool TryCoerceDecimal(object? val, out decimal result)
        {
            result = 0;
            if (val == null) return false;

            switch (val)
            {
                case decimal d:
                    result = d;
                    return true;
                case double dbl:
                    result = (decimal)dbl;
                    return true;
                case float f:
                    result = (decimal)f;
                    return true;
                case int i:
                    result = i;
                    return true;
                case long l:
                    result = l;
                    return true;
                case string s:
                    var cleaned = s.Replace(",", "").Replace("₹", "").Trim();
                    if (string.IsNullOrEmpty(cleaned)) return false;
                    return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
                default:
                    var text = val.ToString()?.Replace(",", "").Replace("₹", "").Trim();
                    return !string.IsNullOrEmpty(text)
                           && decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
            }
        }

        private static bool IsAmountColumn(string header)
        {
            if (string.IsNullOrWhiteSpace(header)) return false;

            var h = header.Trim().ToLowerInvariant();
            if (h is "#" or "id" or "sr" or "sr." or "s.no" or "s no")
                return false;

            if (h.Contains("date") || h.Contains("qty") || h.Contains("quantity") || h.Contains("count") || h.Contains("days"))
                return false;

            if (h.Contains("₹"))
                return true;

            string[] keywords =
            {
                "amount", " amt", "amt ", "debit", "credit", "balance", "price",
                "commission", "refund", "value", "requested", "received", "approved",
                "current", "reserved", "available"
            };

            return keywords.Any(h.Contains);
        }
    }
}
