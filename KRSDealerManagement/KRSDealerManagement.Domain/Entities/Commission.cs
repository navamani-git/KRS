using KRSDealerManagement.Shared.Enums;
using KRSDealerManagement.Shared.Helpers;

namespace KRSDealerManagement.Domain.Entities
{
    /// <summary>
    /// Represents commission earned by subdealer for each vehicle monthly
    /// Commission is calculated per vehicle per month and can vary
    /// </summary>
    public class Commission
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public int CommissionId { get; set; }

        /// <summary>
        /// Reference to SubdealerAccount
        /// </summary>
        public int AccountId { get; set; }

        /// <summary>
        /// Subdealer User ID (denormalized)
        /// </summary>
        public int SubdealerId { get; set; }

        /// <summary>
        /// Reference to Vehicle for which commission is earned
        /// </summary>
        public int VehicleId { get; set; }

        /// <summary>
        /// Month (1-12) for which commission is calculated
        /// </summary>
        public int Month { get; set; }

        /// <summary>
        /// Year (e.g., 2024)
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// Commission amount for this month in rupees
        /// </summary>
        public decimal CommissionAmount { get; set; }

        /// <summary>
        /// Amount approved by admin (maps to ApprovedAmount in live DB)
        /// </summary>
        public decimal? ApprovedAmount { get; set; }

        /// <summary>
        /// Commission status: Pending, Approved, Paid, Rejected
        /// </summary>
        public int Status { get; set; } = (int)CommissionStatusEnum.Pending;

        /// <summary>
        /// Notes about commission calculation/reason
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Admin who approved the commission (null if pending)
        /// </summary>
        public int? ApprovedBy { get; set; }

        /// <summary>
        /// When commission was approved (UTC)
        /// </summary>
        public DateTime? ApprovedDate { get; set; }

        /// <summary>
        /// When commission was credited to account
        /// </summary>
        public DateTime? PaidDate { get; set; }

        /// <summary>
        /// Admin who rejected the commission (null if not rejected)
        /// </summary>
        public int? RejectedBy { get; set; }

        /// <summary>
        /// When commission was rejected (UTC)
        /// </summary>
        public DateTime? RejectedDate { get; set; }

        /// <summary>
        /// Commission creation timestamp (UTC)
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User who submitted the commission
        /// </summary>
        public int SubmittedBy { get; set; }

        /// <summary>
        /// Last modification timestamp (UTC)
        /// </summary>
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Check if commission can be approved
        /// </summary>
        public bool CanBeApproved()
        {
            return CommissionStatusHelper.IsAwaitingApproval(Status, ApprovedDate);
        }

        /// <summary>
        /// Check if commission can be paid
        /// </summary>
        public bool CanBePaid()
        {
            return Status == (int)CommissionStatusEnum.Approved;
        }

        /// <summary>
        /// Approve the commission
        /// </summary>
        public void Approve(int approverUserId)
        {
            if (!CanBeApproved())
                throw new InvalidOperationException($"Cannot approve commission in {GetStatusDisplay()} status");

            Status = (int)CommissionStatusEnum.Approved;
            ApprovedBy = approverUserId;
            ApprovedDate = DateTime.UtcNow;
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Mark commission as paid (credit to account)
        /// </summary>
        public void MarkAsPaid()
        {
            if (!CanBePaid())
                throw new InvalidOperationException($"Cannot pay commission in {GetStatusDisplay()} status");

            Status = (int)CommissionStatusEnum.Paid;
            PaidDate = DateTime.UtcNow;
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Reject the commission
        /// </summary>
        public void Reject(int rejectedBy)
        {
            if (!CommissionStatusHelper.IsAwaitingApproval(Status, ApprovedDate))
                throw new InvalidOperationException($"Cannot reject commission in {GetStatusDisplay()} status");

            Status = (int)CommissionStatusEnum.Rejected;
            RejectedBy = rejectedBy;
            RejectedDate = DateTime.UtcNow;
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Get status as display text
        /// </summary>
        public string GetStatusDisplay()
        {
            return ((CommissionStatusEnum)Status).ToString();
        }

        /// <summary>
        /// Check if commission is in final state
        /// </summary>
        public bool IsFinal()
        {
            return Status == (int)CommissionStatusEnum.Paid || 
                   Status == (int)CommissionStatusEnum.Rejected;
        }

        /// <summary>
        /// Get commission display string
        /// </summary>
        public string GetDisplayInfo()
        {
            return $"{Year}-{Month:D2}: ₹{CommissionAmount:N2} | {GetStatusDisplay()}";
        }

        /// <summary>
        /// Check if this commission is for specified month/year
        /// </summary>
        public bool IsForMonthYear(int month, int year)
        {
            return Month == month && Year == year;
        }
    }
}
