using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>
    /// Assign a dealer-showroom vehicle (after return approval) to a subdealer and debit their wallet.
    /// </summary>
    public class AllocateShowroomVehicleCommand : IRequest<bool>
    {
        public int VehicleId { get; set; }
        /// <summary>Primary subdealer login / wallet user id.</summary>
        public int SubdealerId { get; set; }
        public int AllocatedBy { get; set; }
        public string Remarks { get; set; } = "";
        public int? ReturnRequestId { get; set; }
    }
}
