using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.Helpers
{
    public static class WarrantyPartHelper
    {
        public static bool IsOthersPart(WarrantyPartMaster? part)
        {
            if (part == null) return false;
            return string.Equals(part.PartCode, WarrantyPartCodes.Others, StringComparison.OrdinalIgnoreCase)
                || string.Equals(part.PartName, WarrantyPartCodes.Others, StringComparison.OrdinalIgnoreCase)
                || string.Equals(part.PartName, "Others", StringComparison.OrdinalIgnoreCase);
        }

        public static string? ResolveDisplayName(WarrantyPartMaster? part, string? otherPartName)
        {
            if (part == null) return null;
            if (!IsOthersPart(part)) return part.PartName;
            return string.IsNullOrWhiteSpace(otherPartName) ? part.PartName : otherPartName.Trim();
        }
    }
}
