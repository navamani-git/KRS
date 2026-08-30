namespace KRSDealerManagement.Web.Models
{
    public sealed class ExcelReferenceTable
    {
        public required string Title { get; init; }
        public required IReadOnlyList<string> Headers { get; init; }
        public required IReadOnlyList<IReadOnlyList<object?>> Rows { get; init; }
    }
}
