namespace KRSDealerManagement.Shared.Enums
{
    /// <summary>
    /// Purchase order lifecycle statuses
    /// </summary>
    public enum PurchaseOrderStatusEnum
    {
        /// <summary>
        /// Order awaiting admin approval/rejection
        /// Amount is reserved from subdealer account
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Admin approved the order
        /// Vehicles created, amount debited from account
        /// </summary>
        Approved = 1,

        /// <summary>
        /// Admin rejected the order
        /// Reserved amount released back to account
        /// </summary>
        Rejected = 2,

        /// <summary>
        /// Order delivered to subdealer
        /// </summary>
        Delivered = 3
    }

    /// <summary>
    /// Extension methods for PurchaseOrderStatusEnum
    /// </summary>
    public static class PurchaseOrderStatusEnumExtensions
    {
        public static string GetDisplayName(this PurchaseOrderStatusEnum status)
        {
            return status switch
            {
                PurchaseOrderStatusEnum.Pending => "Pending Approval",
                PurchaseOrderStatusEnum.Approved => "Approved",
                PurchaseOrderStatusEnum.Rejected => "Rejected",
                PurchaseOrderStatusEnum.Delivered => "Delivered",
                _ => "Unknown"
            };
        }

        public static string GetBadgeClass(this PurchaseOrderStatusEnum status)
        {
            return status switch
            {
                PurchaseOrderStatusEnum.Pending => "badge-warning",
                PurchaseOrderStatusEnum.Approved => "badge-info",
                PurchaseOrderStatusEnum.Rejected => "badge-danger",
                PurchaseOrderStatusEnum.Delivered => "badge-success",
                _ => "badge-dark"
            };
        }

        public static bool IsPending(this PurchaseOrderStatusEnum status)
        {
            return status == PurchaseOrderStatusEnum.Pending;
        }

        public static bool IsApproved(this PurchaseOrderStatusEnum status)
        {
            return status == PurchaseOrderStatusEnum.Approved;
        }

        public static bool IsRejected(this PurchaseOrderStatusEnum status)
        {
            return status == PurchaseOrderStatusEnum.Rejected;
        }

        public static bool IsDelivered(this PurchaseOrderStatusEnum status)
        {
            return status == PurchaseOrderStatusEnum.Delivered;
        }

        public static bool IsFinalized(this PurchaseOrderStatusEnum status)
        {
            return status != PurchaseOrderStatusEnum.Pending;
        }
    }
}
