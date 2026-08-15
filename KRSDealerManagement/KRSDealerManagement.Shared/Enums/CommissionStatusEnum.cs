namespace KRSDealerManagement.Shared.Enums
{
    /// <summary>
    /// Commission submission and approval statuses
    /// </summary>
    public enum CommissionStatusEnum
    {
        /// <summary>
        /// Commission submitted by subdealer, awaiting admin approval
        /// Amount not yet added to account balance
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Admin approved the commission
        /// Approved amount will be credited to subdealer account balance
        /// </summary>
        Approved = 1,

        /// <summary>
        /// Commission has been paid/credited to account
        /// </summary>
        Paid = 2,

        /// <summary>
        /// Admin rejected the commission
        /// No amount credited to account
        /// </summary>
        Rejected = 3
    }

    /// <summary>
    /// Extension methods for CommissionStatusEnum
    /// </summary>
    public static class CommissionStatusEnumExtensions
    {
        public static string GetDisplayName(this CommissionStatusEnum status)
        {
            return status switch
            {
                CommissionStatusEnum.Pending => "Pending Approval",
                CommissionStatusEnum.Approved => "Approved",
                CommissionStatusEnum.Paid => "Paid",
                CommissionStatusEnum.Rejected => "Rejected",
                _ => "Unknown"
            };
        }

        public static string GetBadgeClass(this CommissionStatusEnum status)
        {
            return status switch
            {
                CommissionStatusEnum.Pending => "badge-warning",
                CommissionStatusEnum.Approved => "badge-info",
                CommissionStatusEnum.Paid => "badge-success",
                CommissionStatusEnum.Rejected => "badge-danger",
                _ => "badge-dark"
            };
        }

        public static bool IsPending(this CommissionStatusEnum status)
        {
            return status == CommissionStatusEnum.Pending;
        }

        public static bool IsApproved(this CommissionStatusEnum status)
        {
            return status == CommissionStatusEnum.Approved;
        }

        public static bool IsPaid(this CommissionStatusEnum status)
        {
            return status == CommissionStatusEnum.Paid;
        }

        public static bool IsRejected(this CommissionStatusEnum status)
        {
            return status == CommissionStatusEnum.Rejected;
        }
    }
}

