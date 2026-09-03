using KRSDealerManagement.Web.Models;

namespace KRSDealerManagement.Web.Helpers
{
    public static class GridColumnRegistry
    {
        public static IReadOnlyList<GridFilterColumn> Get(string gridId, bool hideLogins = false, bool hideActions = false, bool isSubdealer = false, bool showDealership = false)
        {
            return gridId switch
            {
                GridIds.Subdealers => Subdealers(hideLogins, hideActions),
                GridIds.Vehicles => Vehicles(isSubdealer),
                GridIds.Payments => Payments(includeSubdealer: true),
                GridIds.MyPayments => MyPayments(),
                GridIds.Orders => Orders(),
                GridIds.MyOrders => MyOrders(),
                GridIds.Accounts => Accounts(),
                GridIds.AccountStatement => AccountStatement(),
                GridIds.Returns => Returns(),
                GridIds.MyReturns => MyReturns(),
                GridIds.CommissionApprovals => CommissionApprovals(),
                GridIds.CommissionRates => CommissionRates(),
                GridIds.Dealerships => Dealerships(),
                GridIds.DocumentTypes => DocumentTypes(),
                GridIds.FinanceNames => FinanceNames(),
                GridIds.PaymentTypes => PaymentTypes(),
                GridIds.Prices => Prices(),
                GridIds.RtoLocations => RtoLocations(),
                GridIds.StaffUsers => StaffUsers(),
                GridIds.StatusLookups => StatusLookups(),
                GridIds.VehicleBookings => VehicleBookings(),
                GridIds.VehicleColors => VehicleColors(),
                GridIds.VehicleModels => VehicleModels(),
                GridIds.ShowroomStock => ShowroomStock(),
                GridIds.DealerStock => DealerStock(showDealership),
                _ => Array.Empty<GridFilterColumn>()
            };
        }

        private static List<GridFilterColumn> Subdealers(bool hideLogins, bool hideActions)
        {
            var cols = new List<GridFilterColumn>
            {
                GridFilterColumn.Skip(),
                GridFilterColumn.Combo("name", "Name"),
                GridFilterColumn.Combo("location", "Location")
            };
            if (!hideLogins) cols.Add(GridFilterColumn.Skip());
            cols.Add(GridFilterColumn.Combo("phone", "Phone"));
            cols.Add(GridFilterColumn.Select("status", "Active", "Inactive"));
            cols.Add(GridFilterColumn.DateCol("created", "Created"));
            if (!hideActions) cols.Add(GridFilterColumn.Actions());
            return cols;
        }

        private static List<GridFilterColumn> Vehicles(bool isSubdealer)
        {
            var cols = new List<GridFilterColumn> { GridFilterColumn.Skip() };
            if (!isSubdealer) cols.Add(GridFilterColumn.Combo("subdealer", "Subdealer"));
            cols.AddRange(new[]
            {
                GridFilterColumn.DateCol("orderDate", "Order Date"),
                GridFilterColumn.Combo("orderNumber", "Order #"),
                GridFilterColumn.DateCol("allocated", "Allocated"),
                GridFilterColumn.Combo("model", "Model"),
                GridFilterColumn.Combo("color", "Color"),
                GridFilterColumn.Combo("chassis", "Chassis")
            });
            if (isSubdealer) cols.Add(GridFilterColumn.Select("source", "Dealer", "My order"));
            cols.AddRange(new[]
            {
                GridFilterColumn.Combo("price", "Price"),
                GridFilterColumn.Combo("delivery", "Delivery"),
                GridFilterColumn.Combo("status", "Status"),
                GridFilterColumn.Actions()
            });
            return cols;
        }

        private static List<GridFilterColumn> Payments(bool includeSubdealer)
        {
            var cols = new List<GridFilterColumn> { GridFilterColumn.Skip() };
            if (includeSubdealer) cols.Add(GridFilterColumn.Combo("subdealer", "Subdealer"));
            cols.AddRange(new[]
            {
                GridFilterColumn.Combo("amount", "Amount"),
                GridFilterColumn.Combo("type", "Type"),
                GridFilterColumn.Combo("customer", "Customer"),
                GridFilterColumn.Combo("finance", "Finance"),
                GridFilterColumn.Combo("vin", "VIN"),
                GridFilterColumn.Skip(),
                GridFilterColumn.DateCol("paymentDate", "Payment Date"),
                GridFilterColumn.Combo("status", "Status"),
                GridFilterColumn.DateCol("submitted", "Submitted"),
                GridFilterColumn.DateCol("approved", "Approved"),
                GridFilterColumn.DateCol("received", "Received"),
                GridFilterColumn.Combo("receivedAmt", "Received Amt"),
                GridFilterColumn.Actions()
            });
            return cols;
        }

        private static List<GridFilterColumn> MyPayments() => new()
        {
            GridFilterColumn.Skip(),
            GridFilterColumn.DateCol("paymentDate", "Date"),
            GridFilterColumn.Combo("type", "Type"),
            GridFilterColumn.Combo("amount", "Amount"),
            GridFilterColumn.Combo("customer", "Customer"),
            GridFilterColumn.Combo("finance", "Finance"),
            GridFilterColumn.Combo("vin", "VIN"),
            GridFilterColumn.Skip(),
            GridFilterColumn.Combo("status", "Status"),
            GridFilterColumn.DateCol("submitted", "Submitted"),
            GridFilterColumn.DateCol("approved", "Approved"),
            GridFilterColumn.DateCol("received", "Received"),
            GridFilterColumn.Combo("receivedAmt", "Received Amt"),
            GridFilterColumn.Combo("remarks", "Remarks"),
            GridFilterColumn.Combo("corrections", "Corrections")
        };

        private static List<GridFilterColumn> Orders() => new()
        {
            GridFilterColumn.Combo("orderNumber", "Order No."),
            GridFilterColumn.Combo("subdealer", "Subdealer"),
            GridFilterColumn.DateCol("created", "Created"),
            GridFilterColumn.DateCol("allocated", "Last Allocation"),
            GridFilterColumn.Combo("qty", "Qty"),
            GridFilterColumn.Combo("pending", "Pending"),
            GridFilterColumn.Combo("amount", "Amount"),
            GridFilterColumn.Combo("status", "Status"),
            GridFilterColumn.Combo("notes", "Notes"),
            GridFilterColumn.Actions()
        };

        private static List<GridFilterColumn> MyOrders() => new()
        {
            GridFilterColumn.Combo("orderNumber", "Order No."),
            GridFilterColumn.DateCol("created", "Created"),
            GridFilterColumn.DateCol("allocated", "Last Allocation"),
            GridFilterColumn.Combo("qty", "Qty"),
            GridFilterColumn.Combo("amount", "Amount"),
            GridFilterColumn.Combo("status", "Status"),
            GridFilterColumn.Skip(),
            GridFilterColumn.Combo("notes", "Notes"),
            GridFilterColumn.Actions()
        };

        private static List<GridFilterColumn> Accounts() => new()
        {
            GridFilterColumn.Combo("subdealer", "Subdealer"),
            GridFilterColumn.Combo("current", "Current"),
            GridFilterColumn.Combo("reserved", "Reserved"),
            GridFilterColumn.Combo("available", "Available"),
            GridFilterColumn.Select("status", "Active", "Inactive"),
            GridFilterColumn.Actions()
        };

        private static List<GridFilterColumn> AccountStatement() => new()
        {
            GridFilterColumn.Skip(),
            GridFilterColumn.DateCol("txnDate", "Txn Date"),
            GridFilterColumn.Combo("type", "Type"),
            GridFilterColumn.Combo("description", "Description"),
            GridFilterColumn.Combo("customer", "Customer"),
            GridFilterColumn.Combo("payType", "Pay Type"),
            GridFilterColumn.Combo("finance", "Finance"),
            GridFilterColumn.Combo("vin", "VIN"),
            GridFilterColumn.Combo("requestedAmt", "Requested"),
            GridFilterColumn.Combo("approvedAmt", "Approved"),
            GridFilterColumn.Combo("debit", "Debit"),
            GridFilterColumn.Combo("credit", "Credit"),
            GridFilterColumn.Combo("balance", "Balance"),
            GridFilterColumn.Actions()
        };

        private static List<GridFilterColumn> Returns() => new()
        {
            GridFilterColumn.Skip(),
            GridFilterColumn.Combo("account", "Account"),
            GridFilterColumn.Combo("order", "Order"),
            GridFilterColumn.Combo("vehicle", "Vehicle"),
            GridFilterColumn.Combo("refund", "Refund"),
            GridFilterColumn.Combo("reason", "Reason"),
            GridFilterColumn.Combo("status", "Status"),
            GridFilterColumn.DateCol("requested", "Requested"),
            GridFilterColumn.DateCol("processed", "Processed"),
            GridFilterColumn.Actions()
        };

        private static List<GridFilterColumn> MyReturns() => new()
        {
            GridFilterColumn.Skip(),
            GridFilterColumn.Skip(),
            GridFilterColumn.Combo("order", "Order"),
            GridFilterColumn.Combo("chassis", "Chassis"),
            GridFilterColumn.Combo("account", "Account"),
            GridFilterColumn.Combo("refund", "Refund"),
            GridFilterColumn.Combo("status", "Status"),
            GridFilterColumn.Combo("reason", "Reason"),
            GridFilterColumn.DateCol("requested", "Requested"),
            GridFilterColumn.DateCol("processed", "Processed"),
            GridFilterColumn.DateCol("credited", "Credited"),
            GridFilterColumn.Combo("remarks", "Remarks")
        };

        private static List<GridFilterColumn> CommissionApprovals() => new()
        {
            GridFilterColumn.Skip(),
            GridFilterColumn.Combo("subdealer", "Subdealer"),
            GridFilterColumn.Combo("chassis", "Chassis"),
            GridFilterColumn.Combo("period", "Period"),
            GridFilterColumn.Combo("amount", "Amount"),
            GridFilterColumn.Combo("status", "Status"),
            GridFilterColumn.DateCol("submitted", "Submitted"),
            GridFilterColumn.DateCol("approved", "Approved"),
            GridFilterColumn.DateCol("rejected", "Rejected"),
            GridFilterColumn.Combo("remarks", "Remarks"),
            GridFilterColumn.Actions()
        };

        private static List<GridFilterColumn> CommissionRates() => new()
        {
            GridFilterColumn.Combo("model", "Model"),
            GridFilterColumn.Combo("amount", "Commission"),
            GridFilterColumn.DateCol("from", "From"),
            GridFilterColumn.DateCol("to", "To"),
            GridFilterColumn.Select("status", "Active", "Inactive"),
            GridFilterColumn.Combo("notes", "Notes"),
            GridFilterColumn.DateCol("created", "Created"),
            GridFilterColumn.Actions()
        };

        private static List<GridFilterColumn> Dealerships() => new()
        {
            GridFilterColumn.Combo("code", "Code"),
            GridFilterColumn.Combo("name", "Name"),
            GridFilterColumn.Combo("location", "Location"),
            GridFilterColumn.Combo("phone", "Phone"),
            GridFilterColumn.Combo("subdealers", "Subdealers"),
            GridFilterColumn.Select("status", "Active", "Inactive"),
            GridFilterColumn.Actions()
        };

        private static List<GridFilterColumn> DocumentTypes() => new()
        {
            GridFilterColumn.Skip(),
            GridFilterColumn.Combo("type", "Type"),
            GridFilterColumn.Select("status", "Active", "Inactive"),
            GridFilterColumn.Actions()
        };

        private static List<GridFilterColumn> FinanceNames() => new()
        {
            GridFilterColumn.Skip(),
            GridFilterColumn.Combo("name", "Finance Name"),
            GridFilterColumn.Select("status", "Active", "Inactive"),
            GridFilterColumn.DateCol("created", "Created"),
            GridFilterColumn.Skip()
        };

        private static List<GridFilterColumn> PaymentTypes() => new()
        {
            GridFilterColumn.Skip(),
            GridFilterColumn.Combo("code", "Code"),
            GridFilterColumn.Combo("name", "Name"),
            GridFilterColumn.Combo("finance", "Finance"),
            GridFilterColumn.Combo("sort", "Sort"),
            GridFilterColumn.Select("status", "Active", "Inactive"),
            GridFilterColumn.DateCol("created", "Created"),
            GridFilterColumn.Skip()
        };

        private static List<GridFilterColumn> Prices() => new()
        {
            GridFilterColumn.Combo("model", "Model"),
            GridFilterColumn.Combo("color", "Color"),
            GridFilterColumn.Combo("period", "Period"),
            GridFilterColumn.DateCol("from", "Effective From"),
            GridFilterColumn.Combo("price", "Price"),
            GridFilterColumn.Combo("notes", "Notes"),
            GridFilterColumn.DateCol("updated", "Updated"),
            GridFilterColumn.Actions()
        };

        private static List<GridFilterColumn> RtoLocations() => new()
        {
            GridFilterColumn.Skip(),
            GridFilterColumn.Combo("location", "Location"),
            GridFilterColumn.Select("status", "Active", "Inactive"),
            GridFilterColumn.Actions()
        };

        private static List<GridFilterColumn> StaffUsers() => new()
        {
            GridFilterColumn.Skip(),
            GridFilterColumn.Combo("name", "Name"),
            GridFilterColumn.Combo("role", "Role"),
            GridFilterColumn.Combo("dealership", "Dealership"),
            GridFilterColumn.Combo("username", "Username"),
            GridFilterColumn.Combo("phone", "Phone"),
            GridFilterColumn.Skip(),
            GridFilterColumn.Select("status", "Active", "Inactive"),
            GridFilterColumn.DateCol("created", "Created"),
            GridFilterColumn.Actions()
        };

        private static List<GridFilterColumn> StatusLookups() => new()
        {
            GridFilterColumn.Skip(),
            GridFilterColumn.Combo("category", "Category"),
            GridFilterColumn.Combo("value", "Value"),
            GridFilterColumn.Combo("code", "Code"),
            GridFilterColumn.Combo("name", "Name"),
            GridFilterColumn.Combo("badge", "Badge"),
            GridFilterColumn.Combo("sort", "Sort"),
            GridFilterColumn.Select("status", "Active", "Inactive"),
            GridFilterColumn.Skip()
        };

        private static List<GridFilterColumn> VehicleBookings() => new()
        {
            GridFilterColumn.Combo("id", "ID"),
            GridFilterColumn.Combo("chassis", "Chassis"),
            GridFilterColumn.Combo("subdealer", "Subdealer"),
            GridFilterColumn.Combo("customer", "Customer"),
            GridFilterColumn.Combo("mobile", "Mobile"),
            GridFilterColumn.Combo("status", "Status"),
            GridFilterColumn.DateCol("submitted", "Submitted"),
            GridFilterColumn.DateCol("paperReceived", "Paper Rcvd"),
            GridFilterColumn.DateCol("invoiceDate", "Invoice"),
            GridFilterColumn.DateCol("insuranceDate", "Insurance"),
            GridFilterColumn.Select("invoiceDoc", "Yes", "No"),
            GridFilterColumn.Select("insuranceDoc", "Yes", "No"),
            GridFilterColumn.DateCol("registration", "Registered"),
            GridFilterColumn.Actions()
        };

        private static List<GridFilterColumn> VehicleColors() => new()
        {
            GridFilterColumn.Skip(),
            GridFilterColumn.Combo("color", "Color"),
            GridFilterColumn.Combo("name", "Name"),
            GridFilterColumn.Combo("hex", "Hex"),
            GridFilterColumn.Select("status", "Active", "Inactive"),
            GridFilterColumn.DateCol("created", "Created"),
            GridFilterColumn.Actions()
        };

        private static List<GridFilterColumn> VehicleModels() => new()
        {
            GridFilterColumn.Skip(),
            GridFilterColumn.Combo("name", "Model"),
            GridFilterColumn.Combo("description", "Description"),
            GridFilterColumn.Select("status", "Active", "Inactive"),
            GridFilterColumn.DateCol("created", "Created"),
            GridFilterColumn.Actions()
        };

        private static List<GridFilterColumn> ShowroomStock() => new()
        {
            GridFilterColumn.Skip(),
            GridFilterColumn.Combo("location", "Location"),
            GridFilterColumn.Combo("subdealer", "Subdealer"),
            GridFilterColumn.Combo("chassis", "Chassis"),
            GridFilterColumn.Combo("model", "Model"),
            GridFilterColumn.Combo("color", "Color"),
            GridFilterColumn.Combo("order", "Order #"),
            GridFilterColumn.DateCol("allocated", "Allocated"),
            GridFilterColumn.Combo("days", "Days in stock"),
            GridFilterColumn.Combo("price", "Price")
        };

        private static List<GridFilterColumn> DealerStock(bool showDealership)
        {
            var cols = new List<GridFilterColumn>
            {
                GridFilterColumn.Skip(),
                GridFilterColumn.Combo("dealer", "Branch")
            };
            cols.AddRange(new[]
            {
                GridFilterColumn.Combo("chassis", "Chassis"),
                GridFilterColumn.Combo("model", "Model"),
                GridFilterColumn.Combo("color", "Color"),
                GridFilterColumn.Combo("motor", "Motor"),
                GridFilterColumn.Combo("battery", "Battery"),
                GridFilterColumn.Combo("charger", "Charger"),
                GridFilterColumn.Combo("controller", "Controller"),
                GridFilterColumn.Combo("converter", "Converter"),
                GridFilterColumn.DateCol("received", "Received"),
                GridFilterColumn.DateCol("invoice", "Ampere Invoice"),
                GridFilterColumn.Combo("invoiceNo", "Ampere Invoice No"),
                GridFilterColumn.Combo("allocatedTo", "Allocated To"),
                GridFilterColumn.Select("status", "Available", "Allocated"),
                GridFilterColumn.Actions()
            });
            return cols;
        }
    }
}
