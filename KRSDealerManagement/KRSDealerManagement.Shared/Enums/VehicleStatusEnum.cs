namespace KRSDealerManagement.Shared.Enums
{
    /// <summary>
    /// Vehicle lifecycle and inventory statuses
    /// </summary>
    public enum VehicleStatusEnum
    {
        /// <summary>
        /// Vehicle is available in inventory
        /// </summary>
        Available = 0,

        /// <summary>
        /// Vehicle reserved (pending order approval)
        /// </summary>
        Reserved = 1,

        /// <summary>
        /// Vehicle sold/delivered to customer
        /// </summary>
        Sold = 2,

        /// <summary>
        /// Vehicle damaged and not for sale
        /// </summary>
        Damaged = 3,

        /// <summary>
        /// Vehicle purchased from dealer, awaiting invoice (legacy)
        /// </summary>
        Purchased = 4,

        /// <summary>
        /// Invoice generated, price is locked (legacy)
        /// </summary>
        Invoiced = 5,

        /// <summary>
        /// RTO registration process initiated (legacy)
        /// </summary>
        RTOInitiated = 6,

        /// <summary>
        /// RTO registration number assigned (legacy)
        /// </summary>
        RTONumberGiven = 7
    }

    /// <summary>
    /// Extension methods for VehicleStatusEnum
    /// </summary>
    public static class VehicleStatusEnumExtensions
    {
        public static string GetDisplayName(this VehicleStatusEnum status)
        {
            return status switch
            {
                VehicleStatusEnum.Available => "Available",
                VehicleStatusEnum.Reserved => "Reserved",
                VehicleStatusEnum.Sold => "Sold",
                VehicleStatusEnum.Damaged => "Damaged",
                VehicleStatusEnum.Purchased => "Purchased",
                VehicleStatusEnum.Invoiced => "Invoiced",
                VehicleStatusEnum.RTOInitiated => "RTO Initiated",
                VehicleStatusEnum.RTONumberGiven => "RTO Complete",
                _ => "Unknown"
            };
        }

        public static string GetBadgeClass(this VehicleStatusEnum status)
        {
            return status switch
            {
                VehicleStatusEnum.Available => "badge-success",
                VehicleStatusEnum.Reserved => "badge-warning",
                VehicleStatusEnum.Sold => "badge-dark",
                VehicleStatusEnum.Damaged => "badge-danger",
                VehicleStatusEnum.Purchased => "badge-info",
                VehicleStatusEnum.Invoiced => "badge-warning",
                VehicleStatusEnum.RTOInitiated => "badge-secondary",
                VehicleStatusEnum.RTONumberGiven => "badge-success",
                _ => "badge-dark"
            };
        }

        public static bool CanChangePrice(this VehicleStatusEnum status)
        {
            return status == VehicleStatusEnum.Purchased || status == VehicleStatusEnum.Available;
        }

        public static bool IsEligibleForCommission(this VehicleStatusEnum status)
        {
            return status >= VehicleStatusEnum.Invoiced;
        }

        public static bool IsAvailable(this VehicleStatusEnum status)
        {
            return status == VehicleStatusEnum.Available;
        }

        public static bool IsReserved(this VehicleStatusEnum status)
        {
            return status == VehicleStatusEnum.Reserved;
        }

        public static bool IsSold(this VehicleStatusEnum status)
        {
            return status == VehicleStatusEnum.Sold;
        }

        public static bool IsDamaged(this VehicleStatusEnum status)
        {
            return status == VehicleStatusEnum.Damaged;
        }
    }
}
