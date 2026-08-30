using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Enums;

namespace KRSDealerManagement.Application.Services
{
    public static class RoleTemplateDefaults
    {
        public static IReadOnlyDictionary<string, MenuAccessLevel> GetDefaultMenus(string? templateCode)
        {
            if (string.IsNullOrWhiteSpace(templateCode))
                return new Dictionary<string, MenuAccessLevel>();

            return templateCode.ToUpperInvariant() switch
            {
                RoleTemplateCodes.Manager => new Dictionary<string, MenuAccessLevel>(StringComparer.OrdinalIgnoreCase)
                {
                    [StaffMenuAccess.Subdealers] = MenuAccessLevel.Full,
                    [StaffMenuAccess.Orders] = MenuAccessLevel.Full,
                    [StaffMenuAccess.Vehicles] = MenuAccessLevel.Full,
                    [StaffMenuAccess.DealerStock] = MenuAccessLevel.Full,
                    [StaffMenuAccess.ShowroomStock] = MenuAccessLevel.Full,
                    [StaffMenuAccess.VehicleBookings] = MenuAccessLevel.Full,
                    [StaffMenuAccess.BookedToCustomerView] = MenuAccessLevel.Full,
                    [StaffMenuAccess.Returns] = MenuAccessLevel.Full,
                    [StaffMenuAccess.Balances] = MenuAccessLevel.ReadOnly,
                    [StaffMenuAccess.ChassisHistory] = MenuAccessLevel.ReadOnly,
                },
                RoleTemplateCodes.FinanceManager => new Dictionary<string, MenuAccessLevel>(StringComparer.OrdinalIgnoreCase)
                {
                    [StaffMenuAccess.Balances] = MenuAccessLevel.Full,
                    [StaffMenuAccess.AccountAdjustments] = MenuAccessLevel.Full,
                    [StaffMenuAccess.AccountTransactions] = MenuAccessLevel.Full,
                    [StaffMenuAccess.Payments] = MenuAccessLevel.Full,
                    [StaffMenuAccess.Reports] = MenuAccessLevel.Full,
                },
                RoleTemplateCodes.InsuranceRtoManager => new Dictionary<string, MenuAccessLevel>(StringComparer.OrdinalIgnoreCase)
                {
                    [StaffMenuAccess.VehicleBookings] = MenuAccessLevel.Full,
                    [StaffMenuAccess.BookedToCustomerView] = MenuAccessLevel.Full,
                    [StaffMenuAccess.RtoDistricts] = MenuAccessLevel.ReadOnly,
                    [StaffMenuAccess.RtoLocations] = MenuAccessLevel.ReadOnly,
                    [StaffMenuAccess.ChassisHistory] = MenuAccessLevel.ReadOnly,
                    [StaffMenuAccess.Vehicles] = MenuAccessLevel.ReadOnly,
                },
                _ => new Dictionary<string, MenuAccessLevel>(StringComparer.OrdinalIgnoreCase)
            };
        }

        public static int MapTemplateToLegacyUserRole(string? templateCode)
        {
            return templateCode?.ToUpperInvariant() switch
            {
                RoleTemplateCodes.FinanceManager => 3,
                RoleTemplateCodes.System => 1,
                RoleTemplateCodes.Subdealer => 2,
                _ => 4
            };
        }

        public static string BuildSuggestedRoleCode(string dealershipCode, string templateCode)
        {
            var dealer = Sanitize(dealershipCode);
            var suffix = templateCode.ToUpperInvariant() switch
            {
                RoleTemplateCodes.FinanceManager => "FINANCE_MANAGER",
                RoleTemplateCodes.InsuranceRtoManager => "INSURANCE_RTO_MANAGER",
                RoleTemplateCodes.Manager => "MANAGER",
                _ => "STAFF"
            };
            return $"{dealer}_{suffix}";
        }

        private static string Sanitize(string value)
        {
            var chars = value.Trim().ToUpperInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '_')
                .ToArray();
            return new string(chars).Trim('_');
        }
    }
}
