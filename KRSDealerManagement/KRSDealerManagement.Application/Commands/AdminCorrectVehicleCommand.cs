using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    public class AdminCorrectVehicleCommand : IRequest<bool>
    {
        public int VehicleId { get; set; }
        public int ModelId { get; set; }
        public int ColorId { get; set; }
        public required string ChassisNumber { get; set; }
        public int Status { get; set; }
        public decimal CurrentPrice { get; set; }
        public string? MotorNo { get; set; }
        public string? BatteryNo { get; set; }
        public string? ChargerNo { get; set; }
        public string? ControllerNo { get; set; }
        public string? ConverterNo { get; set; }
        public int? BookingStatus { get; set; }
        public required string CorrectionReason { get; set; }
        public int CorrectedBy { get; set; }
        public required string CorrectedByName { get; set; }
    }
}
