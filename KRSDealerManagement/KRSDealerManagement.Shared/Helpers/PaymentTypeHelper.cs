namespace KRSDealerManagement.Shared.Helpers
{
    public static class PaymentTypeHelper
    {
        /// <summary>Only Finance payments require a customer name.</summary>
        public static bool RequiresCustomerName(string? typeCode) =>
            string.Equals(typeCode, "FINANCE", StringComparison.OrdinalIgnoreCase);

        public static bool ExemptsCustomerName(string? typeCode) =>
            !RequiresCustomerName(typeCode);
    }
}
