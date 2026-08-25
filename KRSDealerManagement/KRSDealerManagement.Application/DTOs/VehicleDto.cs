using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.DTOs
{
    /// <summary>
    /// Vehicle Data Transfer Object
    /// </summary>
    public class VehicleDto
    {
        public int VehicleId { get; set; }
        public int ModelId { get; set; }
        public required string ModelName { get; set; }
        public int ColorId { get; set; }
        public required string ColorName { get; set; }
        public required string ChassisNumber { get; set; }
        public int Status { get; set; }
        public string? StatusName { get; set; }
        public string? StatusBadgeClass { get; set; }
        public int? SubdealerId { get; set; }
        public string? SubdealerName { get; set; }
        public int? PurchaseOrderId { get; set; }
        public string? OrderNumber { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? AllocatedDate { get; set; }
        public decimal CurrentPrice { get; set; }
        public string? MotorNo { get; set; }
        public string? BatteryNo { get; set; }
        public string? ChargerNo { get; set; }
        public string? ControllerNo { get; set; }
        public string? ConverterNo { get; set; }
        public int? ManufacturingYear { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? StockLocation { get; set; }
        public string? Notes { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public DateTime? DeliveryDate { get; set; }

        public int? VehicleBookingId { get; set; }
        public int? BookingStatus { get; set; }
        public string? BookingStatusName { get; set; }
        public string? BookingStatusBadge { get; set; }
        public DateTime? BookingInvoiceDate { get; set; }
        public DateTime? BookingInsuranceDate { get; set; }
        public string? InvoicePath { get; set; }
        public string? InsurancePath { get; set; }
        public bool CreatedByDealer { get; set; }
        public bool HasBooking => VehicleBookingId.HasValue;
        public bool CanBook => !HasBooking && SubdealerId.HasValue
            && UnifiedVehicleStatus.CanStartBooking(Status);
        public bool CanEditBooking => HasBooking && !BookingInvoiceDate.HasValue;
        public bool IsAwaitingDealerApproval => !HasBooking
            && Status == UnifiedVehicleStatus.Submitted;
        public bool CanSubmitSubsidyDocs { get; set; }
        public bool CanMarkDelivered => SubdealerId.HasValue
            && Status != UnifiedVehicleStatus.Delivered;
        public bool IsDelivered => Status == UnifiedVehicleStatus.Delivered;
        public bool CanRequestReturn { get; set; }
        public bool IsReturnPending => Status == UnifiedVehicleStatus.ReturnRequested;

        public bool IsInDeliveryPipeline =>
            HasBooking || UnifiedVehicleStatus.IsBookingPhase(Status);

        public string GetDeliveryStatusDisplay()
        {
            if (IsDelivered)
                return "Delivered";
            return "—";
        }

        public string? GetDeliveryDateTooltip()
            => DeliveryDate?.ToString("yyyy-MM-dd");

        public string GetDeliveryBadgeClass()
        {
            if (IsDelivered)
                return "bg-success";
            return "bg-light text-dark border";
        }

        public string GetStatusDisplay()
            => !string.IsNullOrWhiteSpace(StatusName)
                ? StatusName
                : Status switch
                {
                    0 => "Available",
                    1 => "Reserved",
                    2 => "Sold",
                    3 => "Damaged",
                    _ => "Unknown"
                };

        public string GetBadgeClass()
            => !string.IsNullOrWhiteSpace(StatusBadgeClass)
                ? StatusBadgeClass
                : "bg-secondary";

        public bool IsAvailableForPurchase()
        {
            return Status == 0;
        }

        public string GetDisplayInfo()
        {
            return $"Chassis: {ChassisNumber} | Status: {GetStatusDisplay()}";
        }
    }
}
