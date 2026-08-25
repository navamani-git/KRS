namespace KRSDealerManagement.Domain.Entities
{
    /// <summary>
    /// Represents a payment made by subdealer to dealer
    /// Payment submission for various purposes (account settlement, advance, etc.)
    /// Dealer can approve/reject these payments
    /// </summary>
    public class Payment
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public int PaymentId { get; set; }

        /// <summary>
        /// Reference to SubdealerAccount making payment
        /// </summary>
        public int AccountId { get; set; }

        /// <summary>
        /// Subdealer User ID (denormalized for quick access)
        /// </summary>
        public int SubdealerId { get; set; }

        /// <summary>
        /// Requested payment amount submitted by subdealer (rupees)
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>Amount actually received and credited on approval (may differ from Amount).</summary>
        public decimal? ActualReceivedAmount { get; set; }

        /// <summary>Date payment was actually received in bank/account.</summary>
        public DateTime? ActualReceivedDate { get; set; }

        /// <summary>
        /// Payment method display/code (legacy + TypeName)
        /// </summary>
        public string PaymentType { get; set; }

        public int? PaymentTypeId { get; set; }

        /// <summary>Customer name in CAPS (required for all payment types).</summary>
        public string? CustomerName { get; set; }

        public int? FinanceNameId { get; set; }

        /// <summary>Chassis / VIN number (required when Finance).</summary>
        public string? VinNumber { get; set; }

        /// <summary>Credit request: vehicle model (optional).</summary>
        public string? CreditRequestModelName { get; set; }

        /// <summary>Credit request: vehicle color (optional).</summary>
        public string? CreditRequestColorName { get; set; }

        /// <summary>Relative path under Files/Payment/...</summary>
        public string? PaymentProofPath { get; set; }

        public string? PaymentProof2Path { get; set; }

        /// <summary>
        /// Payment date (when payment was made)
        /// </summary>
        public DateTime PaymentDate { get; set; }

        /// <summary>
        /// Status: Pending, Approved, Rejected
        /// </summary>
        public int Status { get; set; } = 0; // Pending = 0, Approved = 1, Rejected = 2

        /// <summary>
        /// Subdealer's remarks on payment
        /// </summary>
        public string SubdealerRemarks { get; set; }

        /// <summary>
        /// Dealer's remarks on approval/rejection
        /// </summary>
        public string DealerRemarks { get; set; }

        /// <summary>
        /// Dealer who approved/rejected payment
        /// </summary>
        public int? ProcessedBy { get; set; }

        /// <summary>
        /// When payment was approved/rejected
        /// </summary>
        public DateTime? ProcessedDate { get; set; }

        /// <summary>
        /// Whether amount was applied to account (dealer can choose to apply or not)
        /// </summary>
        public bool IsApplied { get; set; } = false;

        /// <summary>
        /// If applied, reference to transaction
        /// </summary>
        public int? TransactionId { get; set; }

        /// <summary>
        /// Payment creation timestamp (UTC)
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last modification timestamp (UTC)
        /// </summary>
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Check if payment can be approved
        /// </summary>
        public bool CanBeApproved()
        {
            return Status == 0; // Pending
        }

        /// <summary>
        /// Check if payment can be rejected
        /// </summary>
        public bool CanBeRejected()
        {
            return Status == 0; // Pending
        }

        /// <summary>
        /// Approve the payment
        /// </summary>
        public void Approve(int approverUserId, string remarks = null)
        {
            if (!CanBeApproved())
                throw new InvalidOperationException($"Cannot approve payment in {GetStatusDisplay()} status");

            Status = 1; // Approved
            ProcessedBy = approverUserId;
            ProcessedDate = DateTime.UtcNow;
            DealerRemarks = remarks;
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Reject the payment
        /// </summary>
        public void Reject(int approverUserId, string remarks = null)
        {
            if (!CanBeRejected())
                throw new InvalidOperationException($"Cannot reject payment in {GetStatusDisplay()} status");

            Status = 2; // Rejected
            ProcessedBy = approverUserId;
            ProcessedDate = DateTime.UtcNow;
            DealerRemarks = remarks;
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Mark payment as applied to account
        /// </summary>
        public void MarkAsApplied(int transactionId)
        {
            if (Status != 1) // Must be approved first
                throw new InvalidOperationException("Only approved payments can be applied");

            IsApplied = true;
            TransactionId = transactionId;
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
        /// Get payment type display
        /// </summary>
        public string GetPaymentTypeDisplay()
        {
            return PaymentType switch
            {
                "Cash" => "Cash",
                "GPay" => "Google Pay",
                "NEFT" => "NEFT",
                "Others" => SubdealerRemarks?.Contains("Other:") == true 
                    ? SubdealerRemarks.Split(":")[1].Trim() 
                    : "Other",
                _ => PaymentType
            };
        }

        /// <summary>
        /// Check if payment is in final state
        /// </summary>
        public bool IsFinal()
        {
            return Status == 1 || Status == 2; // Approved or Rejected
        }

        /// <summary>
        /// Get payment display info
        /// </summary>
        public string GetDisplayInfo()
        {
            return $"₹{Amount:N2} | {GetPaymentTypeDisplay()} | {GetStatusDisplay()}";
        }
    }
}
