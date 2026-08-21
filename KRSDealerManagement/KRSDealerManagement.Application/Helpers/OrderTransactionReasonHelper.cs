namespace KRSDealerManagement.Application.Helpers
{
    public static class OrderTransactionReasonHelper
    {
        public static string Format(string orderNumber, string chassis, string modelName, string colorName)
        {
            var order = orderNumber.Trim();
            var ch = chassis.Trim().ToUpperInvariant();
            var modelColor = $"{modelName.Trim()} — {colorName.Trim()}";
            return $"{order}\n{ch}\n{modelColor}";
        }
    }
}
