using KRSDealerManagement.Shared.Enums;

namespace KRSDealerManagement.Shared.Constants
{
    /// <summary>
    /// Which admin-side menus each staff role can open.
    /// System Admin = all. Finance / Branch Manager = limited.
    /// </summary>
    public static class StaffMenuAccess
    {
        // Admin-area menu keys (not subdealer AccountPermissions)
        public const string VehicleModels = "admin_vehicle_models";
        public const string VehicleColors = "admin_vehicle_colors";
        public const string Prices = "admin_prices";
        public const string Dealers = "admin_dealerships";
        public const string Subdealers = "admin_subdealers";
        public const string Balances = "admin_balances";
        public const string CommissionRates = "admin_commission_rates";
        public const string CommissionApprovals = "admin_commission_approvals";
        public const string Orders = "admin_orders";
        public const string Vehicles = "admin_vehicles";
        public const string Returns = "admin_returns";
        public const string Payments = "admin_payments";
        public const string Reports = "admin_reports";
        public const string StaffUsers = "admin_staff_users";
        public const string FinanceNames = "admin_finance_names";
        public const string PaymentTypes = "admin_payment_types";
        public const string DocumentTypes = "admin_document_types";
        public const string RtoLocations = "admin_rto_locations";
        public const string VehicleBookings = "admin_vehicle_bookings";
        public const string StatusLookups = "admin_status_lookups";

        public static IReadOnlyList<(string Key, string Name)> AllAdminMenus() => new List<(string, string)>
        {
            (VehicleModels, "Vehicle Models"),
            (VehicleColors, "Vehicle Colors"),
            (Prices, "Price Management"),
            (Dealers, "Dealers"),
            (FinanceNames, "Finance Names"),
            (PaymentTypes, "Payment Types"),
            (DocumentTypes, "Document Types"),
            (RtoLocations, "RTO Locations"),
            (StatusLookups, "Status Master"),
            (Subdealers, "Subdealers"),
            (Balances, "Balances"),
            (CommissionRates, "Commission Rates"),
            (CommissionApprovals, "Commission Approvals"),
            (Orders, "Manage Orders"),
            (VehicleBookings, "Vehicle Bookings"),
            (Vehicles, "Subdealer Vehicles"),
            (Returns, "Return Requests"),
            (Payments, "Payment Approvals"),
            (Reports, "Reports"),
            (StaffUsers, "Staff Users")
        };

        public static bool CanAccess(int userRole, string menuKey)
        {
            var role = (UserRoleEnum)userRole;
            if (role == UserRoleEnum.Admin) return true;
            if (role == UserRoleEnum.Subdealer) return false;

            var allowed = role switch
            {
                UserRoleEnum.FinanceAdmin => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    Balances, Payments, Reports
                },
                UserRoleEnum.DealerBranchManager => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    Subdealers, Orders, Vehicles, VehicleBookings, Returns
                },
                _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            };

            return allowed.Contains(menuKey);
        }

        public static IEnumerable<string> GetMenusForRole(int userRole)
            => AllAdminMenus().Where(m => CanAccess(userRole, m.Key)).Select(m => m.Key);

        /// <summary>
        /// Staff/dealer sidebar menu groups (parent → child).
        /// </summary>
        public static IReadOnlyList<MenuGroupDefinition> GetStaffMenuGroups()
        {
            return new List<MenuGroupDefinition>
            {
                new()
                {
                    ParentKey = "management",
                    ParentName = "Management",
                    Icon = "bi-gear",
                    Children = new[]
                    {
                        new MenuItemDefinition
                        {
                            Key = Dealers, Name = "Dealerships",
                            Controller = "Dealerships", Action = "Index", Icon = "bi-geo-alt"
                        },
                        new MenuItemDefinition
                        {
                            Key = StaffUsers, Name = "Staff Users",
                            Controller = "StaffUsers", Action = "Index", Icon = "bi-person-badge",
                            Actions = new[] { "Index", "Create" }
                        },
                        new MenuItemDefinition
                        {
                            Key = VehicleModels, Name = "Vehicle Models",
                            Controller = "VehicleModels", Action = "Index", Icon = "bi-car-front",
                            Actions = new[] { "Index", "Create", "Edit", "Details" }
                        },
                        new MenuItemDefinition
                        {
                            Key = VehicleColors, Name = "Vehicle Colors",
                            Controller = "VehicleColors", Action = "Index", Icon = "bi-palette",
                            Actions = new[] { "Index", "Create", "Edit", "Details" }
                        },
                        new MenuItemDefinition
                        {
                            Key = Prices, Name = "Price Management",
                            Controller = "Prices", Action = "Index", Icon = "bi-currency-rupee",
                            Actions = new[] { "Index", "Create", "Edit" }
                        },
                        new MenuItemDefinition
                        {
                            Key = FinanceNames, Name = "Finance Names",
                            Controller = "FinanceNames", Action = "Index", Icon = "bi-bank",
                            Actions = new[] { "Index", "Create", "Edit" }
                        },
                        new MenuItemDefinition
                        {
                            Key = PaymentTypes, Name = "Payment Types",
                            Controller = "PaymentTypes", Action = "Index", Icon = "bi-wallet2",
                            Actions = new[] { "Index", "Create", "Edit" }
                        },
                        new MenuItemDefinition
                        {
                            Key = DocumentTypes, Name = "Document Types",
                            Controller = "DocumentTypes", Action = "Index", Icon = "bi-card-text"
                        },
                        new MenuItemDefinition
                        {
                            Key = RtoLocations, Name = "RTO Locations",
                            Controller = "RtoLocations", Action = "Index", Icon = "bi-geo"
                        },
                        new MenuItemDefinition
                        {
                            Key = StatusLookups, Name = "Status Master",
                            Controller = "StatusLookups", Action = "Index", Icon = "bi-tags",
                            Actions = new[] { "Index", "Create", "Edit" }
                        },
                        new MenuItemDefinition
                        {
                            Key = Subdealers, Name = "Subdealers",
                            Controller = "Subdealers", Action = "Index", Icon = "bi-people",
                            Actions = new[] { "Index", "Create", "Details" }
                        },
                        new MenuItemDefinition
                        {
                            Key = CommissionRates, Name = "Commission Rates",
                            Controller = "Commissions", Action = "Index", Icon = "bi-cash-coin"
                        }
                    }
                },
                new()
                {
                    ParentKey = "operations",
                    ParentName = "Orders & Operations",
                    Icon = "bi-briefcase",
                    Children = new[]
                    {
                        new MenuItemDefinition
                        {
                            Key = Balances, Name = "Balances",
                            Controller = "Accounts", Action = "Index", Icon = "bi-person-badge",
                            Actions = new[] { "Index", "Statement" }
                        },
                        new MenuItemDefinition
                        {
                            Key = CommissionApprovals, Name = "Commission Approvals",
                            Controller = "Commissions", Action = "Approvals", Icon = "bi-check2-square"
                        },
                        new MenuItemDefinition
                        {
                            Key = Orders, Name = "Manage Orders",
                            Controller = "Orders", Action = "Index", Icon = "bi-cart-check",
                            Actions = new[] { "Index", "Details", "Allocate" }
                        },
                        new MenuItemDefinition
                        {
                            Key = Vehicles, Name = "Subdealer Vehicles",
                            Controller = "Vehicles", Action = "Index", Icon = "bi-ev-front",
                            Actions = new[] { "Index", "AdminEdit" }
                        },
                        new MenuItemDefinition
                        {
                            Key = VehicleBookings, Name = "Vehicle Bookings",
                            Controller = "VehicleBookings", Action = "Index", Icon = "bi-journal-check",
                            Actions = new[] { "Index", "Manage" }
                        },
                        new MenuItemDefinition
                        {
                            Key = Returns, Name = "Return Requests",
                            Controller = "Returns", Action = "Index", Icon = "bi-arrow-return-left"
                        },
                        new MenuItemDefinition
                        {
                            Key = Payments, Name = "Payment Approvals",
                            Controller = "Payments", Action = "Index", Icon = "bi-credit-card",
                            Actions = new[] { "Index", "AdminEdit" }
                        }
                    }
                },
                new()
                {
                    ParentKey = "reports",
                    ParentName = "Reports",
                    Icon = "bi-graph-up",
                    Children = new[]
                    {
                        new MenuItemDefinition
                        {
                            Key = Reports, Name = "Reports",
                            Controller = "Reports", Action = "Index", Icon = "bi-graph-up",
                            Actions = new[] { "Index", "AccountStatement" }
                        }
                    }
                }
            };
        }
    }
}
