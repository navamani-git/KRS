namespace KRSDealerManagement.Web.Models
{
    public class GridFilterColumn
    {
        public required string Key { get; init; }
        public string? Placeholder { get; init; }
        public bool IsDate { get; init; }
        public GridFilterInputType InputType { get; init; } = GridFilterInputType.ComboBox;
        public IReadOnlyList<string>? FixedOptions { get; init; }
        public string? CssClass { get; init; }

        public static GridFilterColumn Skip() => new() { Key = "_", InputType = GridFilterInputType.Text, CssClass = "grid-col-rownum" };
        public static GridFilterColumn Actions() => new() { Key = "_actions", InputType = GridFilterInputType.Text };

        public static GridFilterColumn Combo(string key, string? placeholder = null) => new()
        {
            Key = key,
            Placeholder = placeholder ?? key,
            InputType = GridFilterInputType.ComboBox
        };

        public static GridFilterColumn DateCol(string key, string? placeholder = null) => new()
        {
            Key = key,
            Placeholder = placeholder ?? key,
            IsDate = true,
            InputType = GridFilterInputType.Date
        };

        public static GridFilterColumn Select(string key, params string[] options) => new()
        {
            Key = key,
            Placeholder = key,
            InputType = GridFilterInputType.FixedSelect,
            FixedOptions = options
        };
    }
}
