namespace KRSDealerManagement.Web.Models
{
    public class ExcelImportResult
    {
        public bool Success { get; set; }
        public int InsertedCount { get; set; }
        public string? SavedRelativePath { get; set; }
        public List<ExcelImportError> Errors { get; set; } = new();
    }

    public class ExcelImportError
    {
        public int RowNumber { get; set; }
        public string? Column { get; set; }
        public required string Message { get; set; }
    }

    public class ExcelImportRow
    {
        public int RowNumber { get; set; }
        public IReadOnlyDictionary<string, string> Cells { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string? Get(string key)
            => Cells.TryGetValue(key, out var v) ? v : null;

        public bool IsExampleRow()
        {
            var hint = Get("RowType") ?? Get("_RowType");
            return string.Equals(hint?.Trim(), "EXAMPLE", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsEmpty()
            => Cells.Values.All(string.IsNullOrWhiteSpace);
    }
}
