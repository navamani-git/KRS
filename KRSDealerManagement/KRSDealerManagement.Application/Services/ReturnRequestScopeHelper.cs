using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Application.Services
{
    public static class ReturnRequestScopeHelper
    {
        public static bool BelongsToOrgLoginUsers(
            ReturnRequest request,
            IReadOnlySet<int> orgUserIds,
            IReadOnlyDictionary<int, int> accountSubdealerById,
            IReadOnlyDictionary<int, int?> vehicleSubdealerById,
            IReadOnlyDictionary<int, int> orderSubdealerById)
        {
            if (accountSubdealerById.TryGetValue(request.AccountId, out var accountSubdealerId)
                && orgUserIds.Contains(accountSubdealerId))
            {
                return true;
            }

            if (vehicleSubdealerById.TryGetValue(request.VehicleId, out var vehicleSubdealerId)
                && vehicleSubdealerId.HasValue
                && orgUserIds.Contains(vehicleSubdealerId.Value))
            {
                return true;
            }

            if (orderSubdealerById.TryGetValue(request.OrderId, out var orderSubdealerId)
                && orgUserIds.Contains(orderSubdealerId))
            {
                return true;
            }

            return false;
        }

        public static int CountPending(
            IEnumerable<ReturnRequest> returns,
            IReadOnlySet<int>? orgUserIds,
            IReadOnlyDictionary<int, int> accountSubdealerById,
            IReadOnlyDictionary<int, int?> vehicleSubdealerById,
            IReadOnlyDictionary<int, int> orderSubdealerById)
        {
            var pending = returns.Where(r => r.Status == 0);
            if (orgUserIds == null)
                return pending.Count();

            return pending.Count(r => BelongsToOrgLoginUsers(
                r, orgUserIds, accountSubdealerById, vehicleSubdealerById, orderSubdealerById));
        }
    }
}
