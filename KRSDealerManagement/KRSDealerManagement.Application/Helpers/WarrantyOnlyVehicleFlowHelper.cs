using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.Helpers
{
    public static class WarrantyOnlyVehicleFlowHelper
    {
        public const string Placeholder = "-";
        public const string ExternalSubsidyId = "WARRANTY-ONLY";
        public const string ExternalSaleMarker = "External";

        public static string? DisplayCustomerValue(string? value)
            => string.IsNullOrWhiteSpace(value) || value == Placeholder ? null : value.Trim();

        public static string NormalizeCustomerValue(string? value)
            => string.IsNullOrWhiteSpace(value) ? Placeholder : value.Trim();

        public static async Task EnsureOwnShowroomExistsAsync(IUnitOfWork unitOfWork, int dealershipId)
        {
            if (!await SubdealerOrgService.HasActiveOwnShowroomAsync(unitOfWork, dealershipId))
                throw new InvalidOperationException("Create Own Showroom subdealer first.");
        }

        public static async Task<SubDealer> ValidateOwnShowroomOrgAsync(
            IUnitOfWork unitOfWork,
            int dealershipId,
            int subDealerOrgId)
        {
            var org = await unitOfWork.SubDealers.GetByIdAsync(subDealerOrgId)
                ?? throw new InvalidOperationException("Selected subdealer was not found.");

            if (org.DealershipId != dealershipId)
                throw new InvalidOperationException("Selected subdealer does not belong to this dealership.");

            if (!org.OwnShowroom)
                throw new InvalidOperationException("Select an Own Showroom subdealer.");

            if (!org.IsActive)
                throw new InvalidOperationException("Own Showroom subdealer is inactive.");

            return org;
        }

        public static Task<SubDealer> ResolveOwnShowroomAsync(
            IUnitOfWork unitOfWork,
            int dealershipId,
            int subDealerOrgId)
            => ValidateOwnShowroomOrgAsync(unitOfWork, dealershipId, subDealerOrgId);

        public static async Task<HashSet<int>> GetWarrantyOnlyVehicleIdsAsync(IUnitOfWork unitOfWork)
        {
            var warrantyMasterIds = (await unitOfWork.VehicleMasters.GetAllAsync())
                .Where(m => m.WarrantyOnly)
                .Select(m => m.VehicleMasterId)
                .ToHashSet();

            if (warrantyMasterIds.Count == 0)
                return new HashSet<int>();

            return (await unitOfWork.Vehicles.GetAllAsync())
                .Where(v => warrantyMasterIds.Contains(v.VehicleMasterId))
                .Select(v => v.VehicleId)
                .ToHashSet();
        }

        public static async Task SyncOperationalSubdealerAsync(
            IUnitOfWork unitOfWork,
            VehicleMaster master,
            Vehicle vehicle,
            VehicleBooking? booking)
        {
            if (!vehicle.SubdealerId.HasValue)
                throw new InvalidOperationException("Warranty-only vehicle is not linked to an Own Showroom.");

            await ValidateOwnShowroomOrgAsync(unitOfWork, master.DealershipId, vehicle.SubdealerId.Value);

            ApplyTerminalSoldStatus(vehicle, booking, booking?.SubmittedDate ?? vehicle.DeliveryDate);
            await unitOfWork.Vehicles.UpdateAsync(vehicle);
            if (booking != null)
                await unitOfWork.VehicleBookings.UpdateAsync(booking);
        }

        /// <summary>
        /// Warranty-only vehicles are externally sold before upload. Fill booking milestones with the sale date
        /// and mark vehicle + booking delivered so they never appear in the sales pipeline.
        /// </summary>
        public static void ApplyExternalSaleMilestones(VehicleBooking booking, DateTime saleDate)
        {
            var soldOn = saleDate.Date;

            booking.PaperReceivedDate = soldOn;
            booking.InvoiceDate = soldOn;
            booking.InsuranceDate = soldOn;
            booking.AgentDate = soldOn;
            booking.RegistrationDate = soldOn;
            booking.SubsidyIdDate = soldOn;
            booking.NumberPlateReceivedDate = soldOn;
            booking.NumberPlateReceivedBy = ExternalSaleMarker;
            booking.SubsidyId = ExternalSubsidyId;
            booking.RtoNumber = Placeholder;
            booking.InvoicePath = Placeholder;
            booking.InsurancePath = Placeholder;
            booking.FaceVerificationPath = Placeholder;
            booking.RcImagePath = Placeholder;
            booking.BoothPhotoPath = Placeholder;
            booking.SubsidyUndertakingPath = Placeholder;
            booking.SubsidyDocsSubmittedDate = soldOn;
            booking.BookingStatus = UnifiedVehicleStatus.Delivered;
            booking.ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Warranty-only vehicles are externally sold; mark them delivered so they cannot re-enter booking.
        /// </summary>
        public static void ApplyTerminalSoldStatus(Vehicle vehicle, VehicleBooking? booking, DateTime? saleDate)
        {
            var deliveredOn = saleDate?.Date ?? booking?.SubmittedDate.Date ?? vehicle.DeliveryDate?.Date ?? DateTime.UtcNow.Date;

            if (vehicle.Status != UnifiedVehicleStatus.Delivered)
            {
                vehicle.Status = UnifiedVehicleStatus.Delivered;
                vehicle.DeliveryDate = deliveredOn;
                vehicle.ModifiedDate = DateTime.UtcNow;
            }
            else if (!vehicle.DeliveryDate.HasValue)
            {
                vehicle.DeliveryDate = deliveredOn;
                vehicle.ModifiedDate = DateTime.UtcNow;
            }

            if (string.IsNullOrWhiteSpace(vehicle.RegistrationNumber) || vehicle.RegistrationNumber == Placeholder)
                vehicle.RegistrationNumber = Placeholder;

            if (booking != null)
                ApplyExternalSaleMilestones(booking, deliveredOn);
        }

        public static async Task EnsureNotWarrantyOnlyOperationalVehicleAsync(IUnitOfWork unitOfWork, int vehicleId)
        {
            var vehicle = await unitOfWork.Vehicles.GetByIdAsync(vehicleId);
            if (vehicle == null)
                return;

            var master = await unitOfWork.VehicleMasters.GetByIdAsync(vehicle.VehicleMasterId);
            if (master?.WarrantyOnly == true)
            {
                throw new InvalidOperationException(
                    "This chassis is warranty-only (externally sold) and cannot be booked through the sales process.");
            }
        }

        public static async Task ProvisionSoldVehicleAsync(
            IUnitOfWork unitOfWork,
            VehicleMaster master,
            int subDealerOrgId,
            int createdBy,
            string? customerName,
            string? customerMobile,
            DateTime? saleDate)
        {
            var defaults = await ResolveBookingDefaultsAsync(unitOfWork);
            var submitted = saleDate?.Date ?? DateTime.UtcNow.Date;
            var name = NormalizeCustomerValue(customerName);
            var mobile = NormalizeCustomerValue(customerMobile);

            var vehicle = new Vehicle
            {
                VehicleMasterId = master.VehicleMasterId,
                ModelId = master.ModelId,
                ColorId = master.ColorId,
                ChassisNumber = master.ChassisNumber,
                Status = UnifiedVehicleStatus.Delivered,
                PurchaseOrderId = null,
                SubdealerId = subDealerOrgId,
                CurrentPrice = 0,
                OriginalPrice = 0,
                MotorNo = master.MotorNo,
                BatteryNo = master.BatteryNo,
                ChargerNo = master.ChargerNo,
                ControllerNo = master.ControllerNo,
                ConverterNo = master.ConverterNo,
                ManufacturingYear = 0,
                RegistrationNumber = Placeholder,
                AllocatedDate = submitted,
                DeliveryDate = submitted,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            var vehicleId = await unitOfWork.Vehicles.AddAsync(vehicle);
            await unitOfWork.SubdealerVehicleHistories.AddAsync(new SubdealerVehicleHistory
            {
                SubdealerVehicleId = vehicleId,
                Action = "WarrantyOnlySold",
                Remarks = "Warranty-only external sale",
                UserId = createdBy
            });

            var booking = new VehicleBooking
            {
                VehicleId = vehicleId,
                SubdealerId = subDealerOrgId,
                CustomerName = name,
                CustomerMobile = mobile,
                AlternativeMobile = Placeholder,
                CustomerEmail = Placeholder,
                EAadhaarPath = Placeholder,
                EAadhaarPassword = Placeholder,
                DocumentTypeId = defaults.DocumentTypeId,
                DocumentPath = Placeholder,
                CustomerPhotoPath = Placeholder,
                ChassisPhotoPath = Placeholder,
                CustomerSignPath = Placeholder,
                RtoLocationId = defaults.RtoLocationId,
                FancyNumber = false,
                PaymentMode = "Cash",
                FinanceNameId = defaults.FinanceNameId,
                NomineeName = Placeholder,
                NomineeDob = new DateTime(2000, 1, 1),
                NomineeRelationship = Placeholder,
                SubmittedDate = submitted,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };
            ApplyExternalSaleMilestones(booking, submitted);
            await unitOfWork.VehicleBookings.AddAsync(booking);

            await unitOfWork.VehicleMasters.SetAllocatedAsync(master.VehicleMasterId, true, createdBy);
            await unitOfWork.VehicleMasters.AddHistoryAsync(new VehicleMasterHistory
            {
                VehicleMasterId = master.VehicleMasterId,
                Action = "WarrantyOnlySold",
                Remarks = "Warranty-only sold via Own Showroom",
                UserId = createdBy
            });
        }

        private static async Task<(int DocumentTypeId, int RtoLocationId, int FinanceNameId)> ResolveBookingDefaultsAsync(
            IUnitOfWork unitOfWork)
        {
            var documentTypeId = (await unitOfWork.DocumentTypes.GetAllAsync())
                .Where(d => d.IsActive)
                .OrderBy(d => d.DocumentTypeId)
                .Select(d => d.DocumentTypeId)
                .FirstOrDefault();
            var rtoLocationId = (await unitOfWork.RtoLocations.GetAllAsync())
                .Where(r => r.IsActive)
                .OrderBy(r => r.RtoLocationId)
                .Select(r => r.RtoLocationId)
                .FirstOrDefault();
            var financeNameId = (await unitOfWork.FinanceNames.GetAllAsync())
                .Where(f => f.IsActive)
                .OrderBy(f => f.FinanceNameId)
                .Select(f => f.FinanceNameId)
                .FirstOrDefault();

            if (documentTypeId <= 0 || rtoLocationId <= 0 || financeNameId <= 0)
            {
                throw new InvalidOperationException(
                    "Booking master data is incomplete. Ensure document types, RTO locations, and finance names exist.");
            }

            return (documentTypeId, rtoLocationId, financeNameId);
        }
    }
}
