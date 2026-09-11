namespace KRSDealerManagement.Shared.Helpers
{
    public static class BusinessDateValidation
    {
        public static bool IsFutureDate(DateTime value) => value.Date > IstTime.Today;

        public static string? ValidateNotFuture(DateTime? value, string fieldLabel)
        {
            if (!value.HasValue)
                return null;

            return IsFutureDate(value.Value)
                ? $"{fieldLabel} cannot be in the future."
                : null;
        }

        public static string? ValidateNotFuture(DateTime value, string fieldLabel)
            => ValidateNotFuture((DateTime?)value, fieldLabel);
    }
}
