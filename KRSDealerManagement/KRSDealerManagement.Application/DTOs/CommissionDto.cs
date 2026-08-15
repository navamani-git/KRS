using KRSDealerManagement.Shared.Helpers;

namespace KRSDealerManagement.Application.DTOs
{
    /// <summary>
    /// Commission Data Transfer Object
    /// </summary>
    public class CommissionDto
    {
        public int CommissionId { get; set; }
        public int AccountId { get; set; }
        public required string AccountName { get; set; }
        public int SubdealerId { get; set; }
        public required string SubdealerName { get; set; }
        public int VehicleId { get; set; }
        public required string VehicleChassisNumber { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal CommissionAmount { get; set; }
        public int Status { get; set; }
        public string? StatusName { get; set; }
        public string? StatusBadgeClass { get; set; }
        public string? Notes { get; set; }
        public int? ApprovedBy { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public int? RejectedBy { get; set; }
        public string? RejectedByName { get; set; }
        public DateTime? RejectedDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public string GetStatusDisplay()
            => !string.IsNullOrWhiteSpace(StatusName)
                ? StatusName
                : Status switch
                {
                    0 => "Awaiting Approval",
                    1 => "Approved",
                    2 => "Paid",
                    3 => "Rejected",
                    _ => "Unknown"
                };

        public string GetBadgeClass()
            => !string.IsNullOrWhiteSpace(StatusBadgeClass)
                ? StatusBadgeClass
                : Status switch
                {
                    0 => "bg-warning text-dark",
                    1 => "bg-info",
                    2 => "bg-success",
                    3 => "bg-danger",
                    _ => "bg-secondary"
                };

        public bool CanBeApproved()
        {
            return CommissionStatusHelper.IsAwaitingApproval(Status, ApprovedDate);
        }

        public bool CanBePaid()
        {
            return Status == 1;
        }

        public string GetDisplayInfo()
        {
            return $"{Year}-{Month:D2}: ₹{CommissionAmount:N2} | {GetStatusDisplay()}";
        }

        public bool IsForMonthYear(int month, int year)
        {
            return Month == month && Year == year;
        }
    }
}
