namespace KRSDealerManagement.Web.Helpers
{
    /// <summary>Formats values for HTML datetime-local inputs and display.</summary>
    public static class FormDateTimeHelper
    {
        public static string ToDateTimeLocalValue(DateTime? value)
            => value.HasValue ? value.Value.ToString("yyyy-MM-ddTHH:mm") : "";

        public static string ToDateTimeLocalValue(DateTime value)
            => value.ToString("yyyy-MM-ddTHH:mm");

        public static string FormatDisplay(DateTime? value)
            => value.HasValue ? value.Value.ToString("dd MMM yyyy, HH:mm") : "—";
    }
}
