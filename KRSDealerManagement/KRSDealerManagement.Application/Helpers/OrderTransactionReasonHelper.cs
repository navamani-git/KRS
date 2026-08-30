namespace KRSDealerManagement.Application.Helpers
{
    public static class OrderTransactionReasonHelper
    {
        /// <summary>Account statement description: chassis, model, and color on separate lines.</summary>
        public static string Format(string chassis, string modelName, string colorName)
        {
            var ch = (chassis ?? "").Trim().ToUpperInvariant();
            return $"{ch}\n{modelName.Trim()}\n{colorName.Trim()}";
        }

        /// <summary>Legacy signature — order number is not stored in the description.</summary>
        public static string Format(string orderNumber, string chassis, string modelName, string colorName)
            => Format(chassis, modelName, colorName);
    }
}
