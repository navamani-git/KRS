using System.Globalization;
using System.Text.Json;
using KRSDealerManagement.Shared.Helpers;

namespace KRSDealerManagement.Web.Helpers
{
    public static class AccountTransactionCorrectionDisplayHelper
    {
        public static IReadOnlyList<(string Label, string Value)> ParseSnapshot(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return Array.Empty<(string, string)>();

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var fields = new List<(string, string)>
                {
                    ("Transaction #", GetString(root, "TransactionId")),
                    ("Account #", GetString(root, "AccountId")),
                    ("Type", FormatTransactionType(GetInt(root, "TransactionType"))),
                    ("Amount", FormatMoney(GetDecimal(root, "Amount"))),
                    ("Balance after", FormatMoney(GetDecimal(root, "BalanceAfterTransaction"))),
                    ("Txn date", FormatDate(GetDateTime(root, "CreatedDate"))),
                    ("Description", GetString(root, "Reason")),
                    ("Reference", FormatReference(root)),
                    ("Remarks", GetString(root, "Remarks"))
                };

                if (root.TryGetProperty("Linked", out var linked) && linked.ValueKind != JsonValueKind.Null)
                    AppendLinked(fields, linked);

                return fields.Where(f => !string.IsNullOrWhiteSpace(f.Item2) && f.Item2 != "-").ToList();
            }
            catch
            {
                return new List<(string, string)> { ("Details", "Unable to read saved snapshot.") };
            }
        }

        public static string Summarize(string? json)
        {
            var fields = ParseSnapshot(json);
            var type = fields.FirstOrDefault(f => f.Label == "Type").Value ?? "";
            var amount = fields.FirstOrDefault(f => f.Label == "Amount").Value ?? "";
            var desc = fields.FirstOrDefault(f => f.Label == "Description").Value ?? "";
            if (string.IsNullOrWhiteSpace(desc))
                desc = fields.FirstOrDefault(f => f.Label == "Reference").Value ?? "";
            var parts = new[] { type, amount, desc }.Where(p => !string.IsNullOrWhiteSpace(p));
            return string.Join(" · ", parts);
        }

        private static void AppendLinked(List<(string, string)> fields, JsonElement linked)
        {
            if (linked.TryGetProperty("PaymentId", out _))
            {
                fields.Add(("Payment #", GetString(linked, "PaymentId")));
                fields.Add(("Payment amount", FormatMoney(GetDecimal(linked, "Amount"))));
                fields.Add(("Customer", GetString(linked, "CustomerName")));
                fields.Add(("Payment status", FormatPaymentStatus(GetInt(linked, "Status"))));
                return;
            }

            if (linked.TryGetProperty("CommissionId", out _))
            {
                fields.Add(("Commission #", GetString(linked, "CommissionId")));
                fields.Add(("Commission amount", FormatMoney(GetDecimal(linked, "CommissionAmount"))));
                fields.Add(("Month / Year", $"{GetString(linked, "Month")}/{GetString(linked, "Year")}"));
                return;
            }

            if (linked.TryGetProperty("OrderId", out _))
            {
                fields.Add(("Order #", GetString(linked, "OrderId")));
                fields.Add(("Order total", FormatMoney(GetDecimal(linked, "TotalAmount"))));
                fields.Add(("Order number", GetString(linked, "OrderNumber")));
            }
        }

        private static string FormatReference(JsonElement root)
        {
            var type = GetString(root, "ReferenceType");
            var id = GetString(root, "ReferenceId");
            if (string.IsNullOrWhiteSpace(type) && string.IsNullOrWhiteSpace(id))
                return "";
            if (string.IsNullOrWhiteSpace(id))
                return type;
            return $"{type} #{id}";
        }

        private static string FormatTransactionType(int? type)
        {
            if (!type.HasValue) return "";
            return AccountTransactionTypeHelper.GetDisplayName(type.Value);
        }

        private static string FormatPaymentStatus(int? status) => status switch
        {
            0 => "Pending",
            1 => "Approved",
            2 => "Rejected",
            _ => status?.ToString(CultureInfo.InvariantCulture) ?? ""
        };

        private static string FormatMoney(decimal? value)
            => value.HasValue ? $"₹{value.Value:N2}" : "";

        private static string FormatDate(DateTime? value)
            => value?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "";

        private static string GetString(JsonElement el, string name)
        {
            if (!el.TryGetProperty(name, out var prop))
                return "";
            return prop.ValueKind switch
            {
                JsonValueKind.Number => prop.GetRawText(),
                JsonValueKind.String => prop.GetString() ?? "",
                JsonValueKind.True => "Yes",
                JsonValueKind.False => "No",
                JsonValueKind.Null => "",
                _ => prop.GetRawText()
            };
        }

        private static int? GetInt(JsonElement el, string name)
        {
            if (!el.TryGetProperty(name, out var prop) || prop.ValueKind == JsonValueKind.Null)
                return null;
            if (prop.TryGetInt32(out var i)) return i;
            if (int.TryParse(prop.GetRawText(), out var parsed)) return parsed;
            return null;
        }

        private static decimal? GetDecimal(JsonElement el, string name)
        {
            if (!el.TryGetProperty(name, out var prop) || prop.ValueKind == JsonValueKind.Null)
                return null;
            if (prop.TryGetDecimal(out var d)) return d;
            if (decimal.TryParse(prop.GetRawText(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                return parsed;
            return null;
        }

        private static DateTime? GetDateTime(JsonElement el, string name)
        {
            if (!el.TryGetProperty(name, out var prop) || prop.ValueKind == JsonValueKind.Null)
                return null;
            if (prop.TryGetDateTime(out var dt)) return dt;
            if (DateTime.TryParse(prop.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
                return parsed;
            return null;
        }
    }
}
