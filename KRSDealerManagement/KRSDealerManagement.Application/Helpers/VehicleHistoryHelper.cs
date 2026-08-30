using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.Helpers
{
    public static class VehicleHistoryHelper
    {
        public static string? StatusToAction(int status) => status switch
        {
            UnifiedVehicleStatus.ApprovedByDealer => "Allocated",
            UnifiedVehicleStatus.ReturnRequested => "ReturnRequested",
            UnifiedVehicleStatus.ReturnApproved => "ReturnApproved",
            UnifiedVehicleStatus.ReturnCancelled => "ReturnRejected",
            UnifiedVehicleStatus.BookedToCustomer => "BookedToCustomer",
            UnifiedVehicleStatus.PaperReceived => "PaperReceived",
            UnifiedVehicleStatus.Invoiced => "Invoiced",
            UnifiedVehicleStatus.InsuranceCreated => "InsuranceCreated",
            UnifiedVehicleStatus.RtoRequested => "RtoRequested",
            UnifiedVehicleStatus.Registered => "Registered",
            UnifiedVehicleStatus.SubsidyIdCreated => "SubsidyIdCreated",
            UnifiedVehicleStatus.Delivered => "Delivered",
            _ => null
        };

        public static int? ActionToStatus(string action) => action switch
        {
            "Allocated" => UnifiedVehicleStatus.ApprovedByDealer,
            "Reassigned" => UnifiedVehicleStatus.ApprovedByDealer,
            "ReturnRequested" => UnifiedVehicleStatus.ReturnRequested,
            "ReturnApproved" => UnifiedVehicleStatus.ReturnApproved,
            "ReturnRejected" => UnifiedVehicleStatus.ReturnCancelled,
            "BookedToCustomer" => UnifiedVehicleStatus.BookedToCustomer,
            "PaperReceived" => UnifiedVehicleStatus.PaperReceived,
            "Invoiced" => UnifiedVehicleStatus.Invoiced,
            "InsuranceCreated" => UnifiedVehicleStatus.InsuranceCreated,
            "RtoRequested" => UnifiedVehicleStatus.RtoRequested,
            "Registered" => UnifiedVehicleStatus.Registered,
            "SubsidyIdCreated" => UnifiedVehicleStatus.SubsidyIdCreated,
            "SubsidyDocsSubmitted" => UnifiedVehicleStatus.SubsidyIdCreated,
            "SubsidyDocsUpdated" => UnifiedVehicleStatus.SubsidyIdCreated,
            "BookingEdited" => UnifiedVehicleStatus.BookedToCustomer,
            "NumberPlateReceived" => UnifiedVehicleStatus.Registered,
            "Delivered" => UnifiedVehicleStatus.Delivered,
            "Created" => UnifiedVehicleStatus.Submitted,
            "Imported" => UnifiedVehicleStatus.Submitted,
            "Edited" => UnifiedVehicleStatus.Submitted,
            "Deleted" => UnifiedVehicleStatus.Submitted,
            "Returned" => UnifiedVehicleStatus.Submitted,
            _ => null
        };

        public static Task LogSubdealerEventAsync(
            IUnitOfWork unitOfWork,
            int subdealerVehicleId,
            string action,
            int? userId,
            string? remarks = null)
            => VehicleAllocationHelper.LogSubdealerEventAsync(unitOfWork, subdealerVehicleId, action, userId, remarks);

        public static Task LogStatusChangeAsync(
            IUnitOfWork unitOfWork,
            int subdealerVehicleId,
            int newStatus,
            int? userId,
            string? remarks = null)
        {
            var action = StatusToAction(newStatus);
            if (action == null)
                return Task.CompletedTask;

            return LogSubdealerEventAsync(unitOfWork, subdealerVehicleId, action, userId, remarks);
        }
    }
}
