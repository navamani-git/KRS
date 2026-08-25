using KRSDealerManagement.Domain.ValueObjects;
using KRSDealerManagement.Shared.Enums;

namespace KRSDealerManagement.Domain.Entities
{
    /// <summary>
    /// Represents a physical vehicle in inventory
    /// </summary>
    public class Vehicle
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public int VehicleId { get; set; }

        /// <summary>
        /// Reference to VehicleModel
        /// </summary>
        public int ModelId { get; set; }

        /// <summary>
        /// Reference to VehicleColor
        /// </summary>
        public int ColorId { get; set; }

        /// <summary>
        /// Unique chassis/VIN number (strongly typed value object)
        /// </summary>
        public string ChassisNumber { get; set; }

        /// <summary>
        /// Current status: Available, Sold, Reserved, Damaged / Purchased etc.
        /// </summary>
        public int Status { get; set; } = (int)VehicleStatusEnum.Available;

        public int? PurchaseOrderId { get; set; }
        public int? SubdealerId { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal OriginalPrice { get; set; }

        public string? MotorNo { get; set; }
        public string? BatteryNo { get; set; }
        public string? ChargerNo { get; set; }
        public string? ControllerNo { get; set; }
        public string? ConverterNo { get; set; }

        /// <summary>
        /// Manufacturing year
        /// </summary>
        public int ManufacturingYear { get; set; }

        /// <summary>
        /// Optional registration number
        /// </summary>
        public string RegistrationNumber { get; set; }

        /// <summary>
        /// Current stock location/warehouse
        /// </summary>
        public string StockLocation { get; set; }

        /// <summary>
        /// Additional details/notes
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Admin who added vehicle to inventory
        /// </summary>
        public int CreatedBy { get; set; }

        /// <summary>
        /// Vehicle entry creation timestamp (UTC)
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Admin who last modified vehicle
        /// </summary>
        public int? ModifiedBy { get; set; }

        /// <summary>
        /// Last modification timestamp (UTC)
        /// </summary>
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date vehicle was delivered to customer (local date, set by subdealer).
        /// </summary>
        public DateTime? DeliveryDate { get; set; }

        /// <summary>
        /// Check if vehicle is available for purchase
        /// </summary>
        public bool IsAvailableForPurchase()
        {
            return Status == (int)VehicleStatusEnum.Available;
        }

        /// <summary>
        /// Check if vehicle is reserved/in process
        /// </summary>
        public bool IsReserved()
        {
            return Status == (int)VehicleStatusEnum.Reserved;
        }

        /// <summary>
        /// Mark vehicle as reserved (pending order)
        /// </summary>
        public void MarkAsReserved()
        {
            if (!IsAvailableForPurchase())
                throw new InvalidOperationException($"Cannot reserve vehicle in {GetStatusDisplay()} status");
            
            Status = (int)VehicleStatusEnum.Reserved;
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Mark vehicle as sold
        /// </summary>
        public void MarkAsSold()
        {
            if (!IsReserved() && !IsAvailableForPurchase())
                throw new InvalidOperationException($"Cannot sell vehicle in {GetStatusDisplay()} status");
            
            Status = (int)VehicleStatusEnum.Sold;
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Release reservation (back to available)
        /// </summary>
        public void ReleaseReservation()
        {
            if (!IsReserved())
                throw new InvalidOperationException("Vehicle is not reserved");
            
            Status = (int)VehicleStatusEnum.Available;
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Get status as display text
        /// </summary>
        public string GetStatusDisplay()
        {
            return ((VehicleStatusEnum)Status).ToString();
        }

        /// <summary>
        /// Get vehicle display string
        /// </summary>
        public string GetDisplayInfo()
        {
            return $"Chassis: {ChassisNumber} | Status: {GetStatusDisplay()}";
        }
    }
}
