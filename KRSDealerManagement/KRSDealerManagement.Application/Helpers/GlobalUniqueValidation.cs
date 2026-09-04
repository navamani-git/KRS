using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.Helpers
{
    public static class GlobalUniqueValidation
    {
        public static async Task EnsureChassisAvailableAsync(
            IUnitOfWork unitOfWork,
            string chassisNumber,
            int? excludeVehicleMasterId = null,
            int? excludeVehicleId = null)
        {
            var chassis = chassisNumber.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(chassis) || UnifiedVehicleStatus.IsPlaceholderChassis(chassis))
                return;

            if (await unitOfWork.VehicleMasters.ChassisExistsAsync(chassis, excludeVehicleMasterId))
                throw new InvalidOperationException($"Chassis '{chassis}' already exists.");

            var vehicles = await unitOfWork.Vehicles.GetAllAsync();
            if (vehicles.Any(v =>
                    v.VehicleId != (excludeVehicleId ?? 0)
                    && !UnifiedVehicleStatus.IsPlaceholderChassis(v.ChassisNumber)
                    && string.Equals(v.ChassisNumber?.Trim(), chassis, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Chassis '{chassis}' is already assigned to another vehicle.");
            }
        }

        public static async Task EnsureRtoNumberAvailableAsync(
            IUnitOfWork unitOfWork,
            string? rtoNumber,
            int excludeBookingId)
        {
            var normalized = rtoNumber?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                return;

            var bookings = await unitOfWork.VehicleBookings.GetAllAsync();
            if (bookings.Any(b =>
                    b.VehicleBookingId != excludeBookingId
                    && !string.IsNullOrWhiteSpace(b.RtoNumber)
                    && string.Equals(b.RtoNumber.Trim(), normalized, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"RTO number '{normalized}' is already assigned to another vehicle.");
            }
        }

        public static async Task EnsureSubsidyIdAvailableAsync(
            IUnitOfWork unitOfWork,
            string? subsidyId,
            int excludeBookingId)
        {
            var normalized = subsidyId?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                return;

            var bookings = await unitOfWork.VehicleBookings.GetAllAsync();
            if (bookings.Any(b =>
                    b.VehicleBookingId != excludeBookingId
                    && !string.IsNullOrWhiteSpace(b.SubsidyId)
                    && string.Equals(b.SubsidyId.Trim(), normalized, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Subsidy ID '{normalized}' is already assigned to another vehicle.");
            }
        }
    }
}
