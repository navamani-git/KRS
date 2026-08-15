namespace KRSDealerManagement.Web.Helpers
{
    public static class StatusBadgeOptions
    {
        public static IReadOnlyList<(string Value, string Label)> All => new List<(string, string)>
        {
            ("bg-secondary", "Secondary (gray)"),
            ("bg-warning text-dark", "Warning (yellow)"),
            ("bg-success", "Success (green)"),
            ("bg-danger", "Danger (red)"),
            ("bg-info", "Info (blue)"),
            ("bg-primary", "Primary"),
            ("bg-dark", "Dark")
        };
    }
}
