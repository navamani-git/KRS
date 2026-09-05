using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Application.Helpers
{
    public static class DealershipLocationHelper
    {
        public static string GetLocationLabel(Dealership? dealership)
        {
            if (dealership == null)
                return "Dealer Showroom";

            var location = dealership.Location?.Trim();
            if (!string.IsNullOrWhiteSpace(location))
                return location;

            var name = dealership.DealershipName?.Trim();
            return string.IsNullOrWhiteSpace(name) ? "Dealer Showroom" : name;
        }

        public static int? ResolveDealershipIdFromSubdealerUser(
            int? subdealerUserId,
            IEnumerable<UserOrgRole> orgRoles)
        {
            if (!subdealerUserId.HasValue || subdealerUserId.Value <= 0)
                return null;

            return orgRoles
                .Where(a => a.UserId == subdealerUserId.Value && a.IsActive)
                .OrderByDescending(a => a.IsPrimary)
                .FirstOrDefault()?.DealershipId;
        }

        public static string ResolveShowroomLabel(
            Vehicle? vehicle,
            int? orderSubdealerUserId,
            int? accountSubdealerUserId,
            IReadOnlyDictionary<int, VehicleMaster> masters,
            IReadOnlyDictionary<int, Dealership> dealerships,
            IEnumerable<UserOrgRole> orgRoles)
        {
            int? dealershipId = null;
            if (vehicle?.VehicleMasterId > 0
                && masters.TryGetValue(vehicle.VehicleMasterId, out var master))
            {
                dealershipId = master.DealershipId;
            }

            if (!dealershipId.HasValue)
            {
                dealershipId = ResolveDealershipIdFromSubdealerUser(
                    vehicle?.SubdealerId ?? orderSubdealerUserId ?? accountSubdealerUserId,
                    orgRoles);
            }

            dealerships.TryGetValue(dealershipId ?? 0, out var dealer);
            return GetLocationLabel(dealer);
        }
    }
}
