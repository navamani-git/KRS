using System.ComponentModel.DataAnnotations.Schema;
using KRSDealerManagement.Shared.Enums;

namespace KRSDealerManagement.Domain.Entities
{
    /// <summary>
    /// Represents a vehicle return request from subdealer to dealer
    /// Subdealer can request to return previously purchased vehicle
    /// Upon approval, amount is refunded to subdealer account
    /// </summary>
    public class ReturnRequest
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public int ReturnRequestId { get; set; }

        /// <summary>
        /// Reference to SubdealerAccount requesting return
        /// </summary>
        public int AccountId { get; set; }

        /// <summary>
        /// Reference to PurchaseOrder containing the vehicle
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Reference to Vehicle being returned
        /// </summary>
        [Column("SubdealerVehicleId")]
        public int VehicleId { get; set; }

        /// <summary>
        /// Amount to be refunded (should match original purchase price)
        /// </summary>
        public decimal RefundAmount { get; set; }

        /// <summary>
        /// Status: Pending, Approved, Rejected
        /// </summary>
        public int Status { get; set; } = 0; // Pending = 0, Approved = 1, Rejected = 2

        /// <summary>
        /// Reason for return (subdealer's reason)
        /// </summary>
        public string ReturnReason { get; set; }

        /// <summary>
        /// Admin remarks on approval/rejection
        /// </summary>
        public string AdminRemarks { get; set; }

        /// <summary>
        /// Admin who approved/rejected the return
        /// </summary>
        public int? ProcessedBy { get; set; }

        /// <summary>
        /// When return was approved/rejected
        /// </summary>
        public DateTime? ProcessedDate { get; set; }

        /// <summary>
        /// When return request was created (UTC)
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last modification timestamp (UTC)
        /// </summary>
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Pending when not yet approved/rejected (Status 0 in ReturnRequests table).
        /// Legacy rows may have non-zero Status while vehicle is still ReturnRequested — handler also checks vehicle.
        /// </summary>
        public bool CanBeApproved() => !IsFinal();

        public bool CanBeRejected() => !IsFinal();

        /// <summary>
        /// Approve the return request
        /// </summary>
        public void Approve(int approverUserId, string remarks = null)
        {
            if (IsFinal())
                throw new InvalidOperationException($"Cannot approve return in {GetStatusDisplay()} status");

            Status = 1; // Approved
            ProcessedBy = approverUserId;
            ProcessedDate = DateTime.UtcNow;
            AdminRemarks = remarks;
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Reject the return request
        /// </summary>
        public void Reject(int approverUserId, string remarks = null)
        {
            if (IsFinal())
                throw new InvalidOperationException($"Cannot reject return in {GetStatusDisplay()} status");

            Status = 2; // Rejected
            ProcessedBy = approverUserId;
            ProcessedDate = DateTime.UtcNow;
            AdminRemarks = remarks;
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Get status as display text
        /// </summary>
        public string GetStatusDisplay()
        {
            return Status switch
            {
                0 => "Pending",
                1 => "Approved",
                2 => "Rejected",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Check if return is in final state (approved or rejected)
        /// </summary>
        public bool IsFinal()
        {
            return Status == 1 || Status == 2; // Approved or Rejected
        }

        /// <summary>
        /// Get return request display info
        /// </summary>
        public string GetDisplayInfo()
        {
            return $"Return: ₹{RefundAmount:N2} | {GetStatusDisplay()}";
        }
    }
}
