namespace KRSDealerManagement.Application.DTOs
{
    /// <summary>
    /// Payment Data Transfer Object
    /// </summary>
    public class PaymentDto
    {
        public int PaymentId { get; set; }
        public int AccountId { get; set; }
        public required string AccountName { get; set; }
        public int SubdealerId { get; set; }
        public required string SubdealerName { get; set; }
        public decimal Amount { get; set; }
        /// <summary>Requested amount at submission.</summary>
        public decimal RequestedAmount => Amount;
        public decimal? ActualReceivedAmount { get; set; }
        public DateTime? ActualReceivedDate { get; set; }
        public required string PaymentType { get; set; }
        public int? PaymentTypeId { get; set; }
        public string? CustomerName { get; set; }
        public int? FinanceNameId { get; set; }
        public string? FinanceName { get; set; }
        public string? VinNumber { get; set; }
        public string? PaymentProofPath { get; set; }
        public string? PaymentProof2Path { get; set; }
        public DateTime PaymentDate { get; set; }
        public int Status { get; set; }
        public string? StatusName { get; set; }
        public string? StatusBadgeClass { get; set; }
        public string? SubdealerRemarks { get; set; }
        public string? DealerRemarks { get; set; }
        public int? ProcessedBy { get; set; }
        public string? ProcessedByName { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public bool IsApplied { get; set; }
        public int? TransactionId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        /// <summary>Alias: when payment was submitted.</summary>
        public DateTime SubmittedDate => CreatedDate;
        /// <summary>Alias: when payment was approved/rejected.</summary>
        public DateTime? ApprovedDate => ProcessedDate;

        public string GetStatusDisplay()
            => !string.IsNullOrWhiteSpace(StatusName)
                ? StatusName
                : Status switch
                {
                    0 => "Pending",
                    1 => "Approved",
                    2 => "Rejected",
                    _ => "Unknown"
                };

        public string GetBadgeClass()
            => !string.IsNullOrWhiteSpace(StatusBadgeClass)
                ? StatusBadgeClass
                : Status switch
                {
                    0 => "bg-warning text-dark",
                    1 => "bg-success",
                    2 => "bg-danger",
                    _ => "bg-secondary"
                };

        public string GetPaymentTypeDisplay() => PaymentType ?? "-";

        public bool CanBeApproved() => Status == 0;
        public bool CanBeRejected() => Status == 0;

        public string GetDisplayInfo()
            => $"₹{Amount:N2} | {GetPaymentTypeDisplay()} | {GetStatusDisplay()}";

        public bool IsFinal() => Status == 1 || Status == 2;
    }
}
