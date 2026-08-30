using System.Globalization;

namespace KRSDealerManagement.Shared.Helpers
{
    public static class IndianNumberFormat
    {
        public static string Format(decimal value, int decimals = 2)
        {
            var negative = value < 0;
            value = Math.Abs(value);
            var text = value.ToString($"F{decimals}", CultureInfo.InvariantCulture);
            var parts = text.Split('.');
            var intPart = parts[0];
            var decPart = parts.Length > 1 ? parts[1] : "";

            string formattedInt;
            if (intPart.Length <= 3)
            {
                formattedInt = intPart;
            }
            else
            {
                var last3 = intPart[^3..];
                var rest = intPart[..^3];
                var groups = new List<string>();
                while (rest.Length > 2)
                {
                    groups.Insert(0, rest[^2..]);
                    rest = rest[..^2];
                }

                if (rest.Length > 0)
                    groups.Insert(0, rest);

                formattedInt = string.Join(",", groups) + "," + last3;
            }

            var result = decPart.Length > 0 ? $"{formattedInt}.{decPart}" : formattedInt;
            return negative ? $"-{result}" : result;
        }

        public static string Format(double value, int decimals = 2) => Format((decimal)value, decimals);
    }
}
