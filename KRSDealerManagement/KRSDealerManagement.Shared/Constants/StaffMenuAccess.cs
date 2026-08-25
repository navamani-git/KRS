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
        public const string AccountAdjustments = "admin_account_adjustments";
        public const string AccountTransactions = "admin_account_transactions";
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
        public const string BookedToCustomerView = "admin_booked_to_customer";
        public const string ChassisHistory = "admin_chassis_history";
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
            (AccountAdjustments, "Credit / Debit"),
            (AccountTransactions, "Transaction Corrections"),
            (CommissionRates, "Commission Rates"),
            (CommissionApprovals, "Commission Approvals"),
            (Orders, "Manage Orders"),
            (VehicleBookings, "Vehicle Booking Process"),
            (BookedToCustomerView, "Booked to Customer"),
            (ChassisHistory, "Chassis History"),
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
                    Subdealers, Orders, Vehicles, VehicleBookings, BookedToCustomerView, Returns, Balances
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
                            Key = AccountAdjustments, Name = "Credit / Debit",
                            Controller = "Accounts", Action = "Adjust", Icon = "bi-sliders"
                        },
                        new MenuItemDefinition
                        {
                            Key = AccountTransactions, Name = "Transaction Corrections",
                            Controller = "Accounts", Action = "Transactions", Icon = "bi-journal-text",
                            Actions = new[] { "Transactions", "AdminEditTransaction", "AdminDeleteTransaction", "TransactionCorrections" }
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
                            Actions = new[] { "Index", "AdminEdit", "AdminDelete" }
                        },
                        new MenuItemDefinition
                        {
                            Key = VehicleBookings, Name = "Vehicle Booking Process",
                            Controller = "VehicleBookings", Action = "Process", Icon = "bi-pencil-square",
                            Actions = new[] { "Process", "Manage", "Export" }
                        },
                        new MenuItemDefinition
                        {
                            Key = Returns, Name = "Return Requests",
                            Controller = "Returns", Action = "Index", Icon = "bi-arrow-return-left"
                        },
                        new MenuItemDefinition
                        {
                            Key = ChassisHistory, Name = "Chassis History",
                            Controller = "VehicleHistory", Action = "ChassisHistory", Icon = "bi-clock-history"
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
                    ParentKey = "manage_vehicles",
                    ParentName = "Manage Vehicles",
                    Icon = "bi-journal-check",
                    Children = GetManageVehiclesMenuItems()
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

        private static IReadOnlyList<MenuItemDefinition> GetManageVehiclesMenuItems() => new[]
        {
            new MenuItemDefinition
            {
                Key = BookedToCustomerView,
                Name = "Booked to Customer",
                Controller = "VehicleBookings",
                Action = "BookedToCustomer",
                Icon = "bi-list-check",
                Actions = new[] { "BookedToCustomer", "Export" }
            },
            BookingMenuItem("Paper Received", UnifiedVehicleStatus.PaperReceived, "bi-file-earmark-text"),
            BookingMenuItem("Invoiced", UnifiedVehicleStatus.Invoiced, "bi-receipt"),
            BookingMenuItem("Insurance Created", UnifiedVehicleStatus.InsuranceCreated, "bi-shield-check"),
            BookingMenuItem("RTO Requested", UnifiedVehicleStatus.RtoRequested, "bi-signpost"),
            BookingMenuItem("Registered", UnifiedVehicleStatus.Registered, "bi-card-checklist")
        };

        private static MenuItemDefinition BookingMenuItem(string name, int status, string icon) => new()
        {
            Key = VehicleBookings,
            Name = name,
            Controller = "VehicleBookings",
            Action = "Index",
            Icon = icon,
            Actions = new[] { "Index", "Export" },
            RouteValues = new Dictionary<string, object> { ["status"] = status }
        };
    }
}
