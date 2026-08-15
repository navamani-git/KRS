namespace KRSDealerManagement.Shared.Constants
{
    /// <summary>
    /// Menu/Feature keys used for permission checking and authorization
    /// Used in AccountPermission configuration
    /// </summary>
    public static class MenuKeys
    {
        // Dashboard & Home
        public const string Dashboard = "dashboard";
        public const string Home = "home";

        // Purchase Orders (Subdealer features)
        public const string PurchaseOrders = "purchase_orders";
        public const string PurchaseOrderCreate = "purchase_orders_create";
        public const string PurchaseOrderView = "purchase_orders_view";
        public const string PurchaseOrderEdit = "purchase_orders_edit";
        public const string PurchaseOrderApprove = "purchase_orders_approve";

        // Commissions (Subdealer features)
        public const string Commissions = "commissions";
        public const string CommissionSubmit = "commissions_submit";
        public const string CommissionView = "commissions_view";
        public const string CommissionApprove = "commissions_approve";

        // Vehicles (Subdealer features)
        public const string Vehicles = "vehicles";
        public const string VehiclesView = "vehicles_view";
        public const string VehiclesCreate = "vehicles_create";
        public const string VehiclesEdit = "vehicles_edit";
        public const string MyReturns = "my_returns";

        // Account Management (Subdealer features)
        public const string Account = "account";
        public const string AccountBalance = "account_balance";
        public const string AccountTransactions = "account_transactions";
        public const string AccountStatements = "account_statements";
        public const string MyPayments = "my_payments";
        public const string Reports = "reports";

        // Admin Features
        public const string AdminPanel = "admin_panel";
        public const string SubdealerManagement = "subdealer_management";
        public const string AccountManagement = "account_management";
        public const string PermissionManagement = "permission_management";
        public const string PriceManagement = "price_management";
        public const string VehicleManagement = "vehicle_management";
        public const string ReportsAdmin = "reports_admin";

        /// <summary>
        /// Get all available menu keys for configuration
        /// </summary>
        public static List<string> GetAllMenuKeys()
        {
            return new List<string>
            {
                Dashboard, Home,
                PurchaseOrders, PurchaseOrderCreate, PurchaseOrderView, PurchaseOrderEdit, PurchaseOrderApprove,
                Commissions, CommissionSubmit, CommissionView, CommissionApprove,
                Vehicles, VehiclesView, VehiclesCreate, VehiclesEdit, MyReturns,
                Account, AccountBalance, AccountTransactions, AccountStatements, MyPayments, Reports,
                AdminPanel, SubdealerManagement, AccountManagement, PermissionManagement,
                PriceManagement, VehicleManagement, ReportsAdmin
            };
        }

        /// <summary>
        /// Menus grouped by parent for subdealer permission UI and sidebar.
        /// </summary>
        public static IReadOnlyList<MenuGroupDefinition> GetSubdealerMenuGroups()
        {
            return new List<MenuGroupDefinition>
            {
                new()
                {
                    ParentKey = "account",
                    ParentName = "My Account",
                    Icon = "bi-wallet2",
                    Children = new[]
                    {
                        new MenuItemDefinition
                        {
                            Key = AccountStatements, Name = GetDisplayName(AccountStatements),
                            DefaultAccessible = true, Controller = "Account", Action = "Statement", Icon = "bi-file-text"
                        }
                    }
                },
                new()
                {
                    ParentKey = "orders",
                    ParentName = "Orders",
                    Icon = "bi-cart",
                    Children = new[]
                    {
                        new MenuItemDefinition
                        {
                            Key = PurchaseOrderCreate, Name = GetDisplayName(PurchaseOrderCreate),
                            DefaultAccessible = true, Controller = "Orders", Action = "Create", Icon = "bi-cart-plus"
                        },
                        new MenuItemDefinition
                        {
                            Key = PurchaseOrderView, Name = GetDisplayName(PurchaseOrderView),
                            DefaultAccessible = true, Controller = "Orders", Action = "MyOrders", Icon = "bi-list-check"
                        }
                    }
                },
                new()
                {
                    ParentKey = "vehicles",
                    ParentName = "Vehicles",
                    Icon = "bi-ev-front",
                    Children = new[]
                    {
                        new MenuItemDefinition
                        {
                            Key = VehiclesView, Name = GetDisplayName(VehiclesView),
                            DefaultAccessible = true, Controller = "Vehicles", Action = "Index", Icon = "bi-ev-front"
                        },
                        new MenuItemDefinition
                        {
                            Key = MyReturns, Name = GetDisplayName(MyReturns),
                            DefaultAccessible = true, Controller = "Returns", Action = "MyReturns", Icon = "bi-arrow-return-left"
                        }
                    }
                },
                new()
                {
                    ParentKey = "commission_payments",
                    ParentName = "Commission & Payments",
                    Icon = "bi-cash-stack",
                    Children = new[]
                    {
                        new MenuItemDefinition
                        {
                            Key = CommissionSubmit, Name = GetDisplayName(CommissionSubmit),
                            DefaultAccessible = true, Controller = "Commissions", Action = "Submit", Icon = "bi-cash-stack"
                        },
                        new MenuItemDefinition
                        {
                            Key = MyPayments, Name = GetDisplayName(MyPayments),
                            DefaultAccessible = true, Controller = "Payments", Action = "MyPayments", Icon = "bi-wallet2"
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
                            Key = Reports, Name = GetDisplayName(Reports),
                            DefaultAccessible = true, Controller = "Reports", Action = "Index", Icon = "bi-graph-up"
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Menus an admin can grant/revoke for a subdealer account.
        /// </summary>
        public static IReadOnlyList<(string Key, string Name, bool DefaultAccessible)> GetSubdealerConfigurableMenus()
            => GetSubdealerMenuGroups()
                .SelectMany(g => g.Children.Select(c => (c.Key, c.Name, c.DefaultAccessible)))
                .ToList();

        /// <summary>
        /// Get menu display name for UI
        /// </summary>
        public static string GetDisplayName(string menuKey)
        {
            return menuKey switch
            {
                Dashboard => "Dashboard",
                Home => "Home",
                PurchaseOrders => "Purchase Orders",
                PurchaseOrderCreate => "Create Purchase Order",
                PurchaseOrderView => "View Purchase Orders",
                PurchaseOrderEdit => "Edit Purchase Orders",
                PurchaseOrderApprove => "Approve Purchase Orders",
                Commissions => "Commissions",
                CommissionSubmit => "Submit Commission",
                CommissionView => "View Commissions",
                CommissionApprove => "Approve Commissions",
                Vehicles => "Vehicles",
                VehiclesView => "View Vehicles",
                MyReturns => "My Returns",
                VehiclesCreate => "Create Vehicles",
                VehiclesEdit => "Edit Vehicles",
                Account => "Account",
                AccountBalance => "View Balance",
                AccountTransactions => "Transaction History",
                AccountStatements => "Account Statement",
                MyPayments => "My Payments",
                Reports => "Reports",
                AdminPanel => "Admin Panel",
                SubdealerManagement => "Subdealer Management",
                AccountManagement => "Account Management",
                PermissionManagement => "Permission Management",
                PriceManagement => "Price Management",
                VehicleManagement => "Vehicle Management",
                ReportsAdmin => "Reports",
                _ => menuKey
            };
        }
    }
}
