using KRSDealerManagement.Shared.Enums;

namespace KRSDealerManagement.Shared.Helpers
{
    /// <summary>
    /// Normalizes commission status between legacy DB values (1=Pending, 2=Approved, 3=Rejected)
    /// and current app values (0=Pending, 1=Approved, 2=Paid, 3=Rejected).
    /// </summary>
    public static class CommissionStatusHelper
    {
        public static int Normalize(int status, DateTime? approvedDate, decimal? approvedAmount)
        {
            if (status == 1 && !approvedDate.HasValue)
                return (int)CommissionStatusEnum.Pending;

            if (status == 2 && approvedDate.HasValue)
                return approvedAmount.HasValue
                    ? (int)CommissionStatusEnum.Paid
                    : (int)CommissionStatusEnum.Approved;

            if (status == 3)
                return (int)CommissionStatusEnum.Rejected;

            return status;
        }

        public static bool IsAwaitingApproval(int status, DateTime? approvedDate)
            => Normalize(status, approvedDate, null) == (int)CommissionStatusEnum.Pending;

        public static bool IsFinal(int status, DateTime? approvedDate, decimal? approvedAmount)
        {
            var normalized = Normalize(status, approvedDate, approvedAmount);
            return normalized == (int)CommissionStatusEnum.Paid
                || normalized == (int)CommissionStatusEnum.Rejected;
        }
    }
}
