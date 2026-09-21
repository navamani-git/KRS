using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Application.Services
{
    public static class ReturnRequestScopeHelper
    {
        public static bool BelongsToOrg(
            ReturnRequest request,
            int orgId,
            IReadOnlySet<int> orgLoginUserIds,
            IReadOnlyDictionary<int, int> accountSubdealerById,
            IReadOnlyDictionary<int, int?> vehicleSubdealerById,
            IReadOnlyDictionary<int, int> orderSubdealerById)
        {
            if (accountSubdealerById.TryGetValue(request.AccountId, out var accountSubdealerId)
                && orgLoginUserIds.Contains(accountSubdealerId))
            {
                return true;
            }

            if (vehicleSubdealerById.TryGetValue(request.VehicleId, out var vehicleSubdealerId)
                && vehicleSubdealerId.HasValue
                && vehicleSubdealerId.Value == orgId)
            {
                return true;
            }

            if (orderSubdealerById.TryGetValue(request.OrderId, out var orderSubdealerId)
                && orderSubdealerId == orgId)
            {
                return true;
            }

            return false;
        }

        public static int CountPending(
            IEnumerable<ReturnRequest> returns,
            int? orgId,
            IReadOnlySet<int>? orgLoginUserIds,
            IReadOnlyDictionary<int, int> accountSubdealerById,
            IReadOnlyDictionary<int, int?> vehicleSubdealerById,
            IReadOnlyDictionary<int, int> orderSubdealerById)
        {
            var pending = returns.Where(r => r.Status == 0);
            if (!orgId.HasValue || orgLoginUserIds == null)
                return pending.Count();

            return pending.Count(r => BelongsToOrg(
                r, orgId.Value, orgLoginUserIds, accountSubdealerById, vehicleSubdealerById, orderSubdealerById));
        }
    }
}
