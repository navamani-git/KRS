namespace KRSDealerManagement.Application.Helpers
{
    public static class OrderTransactionReasonHelper
    {
        /// <summary>Account statement description: model, color, and chassis on separate lines.</summary>
        public static string Format(string modelName, string colorName, string chassis)
        {
            var ch = (chassis ?? "").Trim().ToUpperInvariant();
            return $"{modelName.Trim()}\n{colorName.Trim()}\n{ch}";
        }

        /// <summary>Legacy call sites — order number is ignored.</summary>
        public static string Format(string orderNumber, string chassis, string modelName, string colorName)
            => Format(modelName, colorName, chassis);
    }
}
