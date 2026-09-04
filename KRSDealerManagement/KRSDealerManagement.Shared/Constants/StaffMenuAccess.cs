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
        public const string StaffRoles = "admin_staff_roles";
        public const string RoleTemplates = "admin_role_templates";
        public const string FinanceNames = "admin_finance_names";
        public const string PaymentTypes = "admin_payment_types";
        public const string DocumentTypes = "admin_document_types";
        public const string RtoDistricts = "admin_rto_districts";
        public const string RtoLocations = "admin_rto_locations";
        public const string VehicleBookings = "admin_vehicle_bookings";
        public const string BookedToCustomerView = "admin_booked_to_customer";
        public const string BookingPaperReceived = "admin_booking_paper_received";
        public const string BookingInvoiced = "admin_booking_invoiced";
        public const string BookingInsuranceCreated = "admin_booking_insurance_created";
        public const string BookingRtoRequested = "admin_booking_rto_requested";
        public const string BookingSubsidyIdPending = "admin_booking_subsidy_id_pending";
        public const string BookingSubsidyDocsPending = "admin_booking_subsidy_docs_pending";
        public const string BookingRegistered = "admin_booking_registered";
        public const string ChassisHistory = "admin_chassis_history";
        public const string ShowroomStock = "admin_showroom_stock";
        public const string DealerStock = "admin_dealer_stock";
        public const string StatusLookups = "admin_status_lookups";
        public const string WarrantyClaims = "admin_warranty_claims";
        public const string WarrantyParts = "admin_warranty_parts";

        public static IReadOnlyList<(string Key, string Name)> AllAdminMenus() => new List<(string, string)>
        {
            (VehicleModels, "Vehicle Models"),
            (VehicleColors, "Vehicle Colors"),
            (Prices, "Price Management"),
            (Dealers, "Dealers"),
            (FinanceNames, "Finance Names"),
            (PaymentTypes, "Payment Types"),
            (DocumentTypes, "Document Types"),
            (RtoDistricts, "RTO Districts"),
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
            (BookingPaperReceived, "Paper Received"),
            (BookingInvoiced, "Invoiced"),
            (BookingInsuranceCreated, "Insurance Created"),
            (BookingRtoRequested, "RTO Requested"),
            (BookingSubsidyIdPending, "Subsidy ID Pending"),
            (BookingSubsidyDocsPending, "Subsidy Docs Pending"),
            (BookingRegistered, "Registered"),
            (ChassisHistory, "Chassis History"),
            (Vehicles, "Subdealer Vehicles"),
            (DealerStock, "Dealer Stock"),
            (ShowroomStock, "Subdealer Stock"),
            (Returns, "Return Requests"),
            (WarrantyClaims, "Warranty Claims"),
            (WarrantyParts, "Warranty Parts"),
            (Payments, "Payment Approvals"),
            (Reports, "Reports"),
            (StaffUsers, "Staff Users"),
            (StaffRoles, "Staff Roles"),
            (RoleTemplates, "Role Templates")
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
                    Subdealers, Orders, Vehicles, DealerStock, ShowroomStock, VehicleBookings, BookedToCustomerView, Returns, WarrantyClaims, Balances
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
                            Key = StaffRoles, Name = "Staff Roles",
                            Controller = "StaffRoles", Action = "Index", Icon = "bi-shield-lock",
                            Actions = new[] { "Index", "Create", "Edit" }
                        },
                        new MenuItemDefinition
                        {
                            Key = RoleTemplates, Name = "Role Templates",
                            Controller = "RoleTemplates", Action = "Index", Icon = "bi-diagram-3",
                            Actions = new[] { "Index", "Create", "Edit" }
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
                            Key = RtoDistricts, Name = "RTO Districts",
                            Controller = "RtoDistricts", Action = "Index", Icon = "bi-map",
                            Actions = new[] { "Index", "Create", "Edit" }
                        },
                        new MenuItemDefinition
                        {
                            Key = RtoLocations, Name = "RTO Locations",
                            Controller = "RtoLocations", Action = "Index", Icon = "bi-geo",
                            Actions = new[] { "Index", "Create", "Edit" }
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
                            Key = DealerStock, Name = "Dealer Stock",
                            Controller = "VehicleMasters", Action = "Index", Icon = "bi-boxes",
                            Actions = new[] { "Index", "Create", "Edit" }
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
                            Key = ShowroomStock, Name = "Subdealer Stock",
                            Controller = "Stock", Action = "Index", Icon = "bi-box-seam",
                            Actions = new[] { "Index" }
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
                    ParentKey = "warranty",
                    ParentName = "Warranty",
                    Icon = "bi-shield-check",
                    Children = new[]
                    {
                        new MenuItemDefinition
                        {
                            Key = WarrantyClaims, Name = "Warranty Claims",
                            Controller = "WarrantyClaims", Action = "Index", Icon = "bi-clipboard-check",
                            Actions = new[] { "Index", "Details", "Approve", "Reject", "RequestInfo", "Export" }
                        },
                        new MenuItemDefinition
                        {
                            Key = WarrantyParts, Name = "Warranty Parts",
                            Controller = "WarrantyParts", Action = "Index", Icon = "bi-tools",
                            Actions = new[] { "Index", "Create", "Edit" }
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
            new MenuItemDefinition
            {
                Key = BookingSubsidyIdPending,
                Name = "Subsidy ID Pending",
                Controller = "VehicleBookings",
                Action = "SubsidyIdPending",
                Icon = "bi-tag",
                Actions = new[] { "SubsidyIdPending", "Export" }
            },
            new MenuItemDefinition
            {
                Key = BookingSubsidyDocsPending,
                Name = "Subsidy Docs Pending",
                Controller = "VehicleBookings",
                Action = "SubsidyDocsPending",
                Icon = "bi-file-earmark-person",
                Actions = new[] { "SubsidyDocsPending", "SubsidyDocs", "ExportSubsidyDocsPending" }
            },
            new MenuItemDefinition
            {
                Key = BookingRegistered,
                Name = "Registered",
                Controller = "VehicleBookings",
                Action = "RegisteredAwaitingPlate",
                Icon = "bi-card-checklist",
                Actions = new[] { "RegisteredAwaitingPlate", "NumberPlateReceived", "ExportRegisteredAwaitingPlate" }
            }
        };

        public static IReadOnlyList<string> BookingMilestoneMenuKeys => new[]
        {
            BookingPaperReceived,
            BookingInvoiced,
            BookingInsuranceCreated,
            BookingRtoRequested,
            BookingSubsidyIdPending,
            BookingSubsidyDocsPending,
            BookingRegistered
        };

        public static bool IsBookingMilestoneKey(string menuKey)
            => BookingMilestoneMenuKeys.Contains(menuKey, StringComparer.OrdinalIgnoreCase);

        public static string? GetMilestoneMenuKeyForStatus(int status) => status switch
        {
            UnifiedVehicleStatus.PaperReceived => BookingPaperReceived,
            UnifiedVehicleStatus.Invoiced => BookingInvoiced,
            UnifiedVehicleStatus.InsuranceCreated => BookingInsuranceCreated,
            UnifiedVehicleStatus.RtoRequested => BookingRtoRequested,
            UnifiedVehicleStatus.Registered => BookingRegistered,
            _ => null
        };

        public static string? GetMilestoneMenuKeyForAction(string action) => action switch
        {
            "SubsidyIdPending" => BookingSubsidyIdPending,
            "SubsidyDocsPending" or "SubsidyDocs" or "ExportSubsidyDocsPending" => BookingSubsidyDocsPending,
            "RegisteredAwaitingPlate" or "NumberPlateReceived" or "ExportRegisteredAwaitingPlate" => BookingRegistered,
            _ => null
        };

        public static IReadOnlyList<string> AllBookingStaffMenuKeys()
        {
            var keys = new List<string> { VehicleBookings, BookedToCustomerView };
            keys.AddRange(BookingMilestoneMenuKeys);
            return keys;
        }

        private static MenuItemDefinition BookingMenuItem(string name, int status, string icon)
        {
            var key = GetMilestoneMenuKeyForStatus(status) ?? VehicleBookings;
            return new MenuItemDefinition
            {
                Key = key,
                Name = name,
                Controller = "VehicleBookings",
                Action = "Index",
                Icon = icon,
                Actions = new[] { "Index", "Export" },
                RouteValues = new Dictionary<string, object> { ["status"] = status }
            };
        }

        public static bool TryResolveMenuKey(string controller, string action, out string menuKey)
            => TryResolveMenuKey(controller, action, null, null, out menuKey);

        public static bool TryResolveMenuKey(
            string controller,
            string action,
            string? returnController,
            string? returnAction,
            out string menuKey)
        {
            menuKey = "";
            if (string.Equals(controller, "ExcelImport", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(returnController))
            {
                controller = returnController;
                if (!string.IsNullOrWhiteSpace(returnAction))
                    action = returnAction;
            }

            if (string.IsNullOrWhiteSpace(controller) || string.IsNullOrWhiteSpace(action))
                return false;

            if (action.StartsWith("Export", StringComparison.OrdinalIgnoreCase))
                return TryResolveMenuKeyForExport(controller, out menuKey);

            foreach (var group in GetStaffMenuGroups())
            {
                foreach (var item in group.Children)
                {
                    if (!string.Equals(item.Controller, controller, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (item.Actions is { Count: > 0 })
                    {
                        if (item.Actions.Any(a => string.Equals(a, action, StringComparison.OrdinalIgnoreCase)))
                        {
                            menuKey = item.Key;
                            return true;
                        }
                    }
                    else if (string.Equals(item.Action, action, StringComparison.OrdinalIgnoreCase))
                    {
                        menuKey = item.Key;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryResolveMenuKeyForExport(string controller, out string menuKey)
        {
            menuKey = "";
            foreach (var group in GetStaffMenuGroups())
            {
                foreach (var item in group.Children)
                {
                    if (!string.Equals(item.Controller, controller, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (item.Actions?.Any(a => a.StartsWith("Export", StringComparison.OrdinalIgnoreCase)) == true
                        || string.Equals(item.Action, "Index", StringComparison.OrdinalIgnoreCase))
                    {
                        menuKey = item.Key;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
