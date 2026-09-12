using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Application.Helpers
{
    public static class DealershipQueryScope
    {
        public static HashSet<int>? ResolveDealershipIds(int? singleDealershipId, IList<int>? dealershipIds)
        {
            if (dealershipIds is { Count: > 0 })
                return dealershipIds.ToHashSet();

            if (singleDealershipId.HasValue)
                return new HashSet<int> { singleDealershipId.Value };

            return null;
        }

        public static bool MatchesDealership(int dealershipId, HashSet<int>? filter)
            => filter == null || filter.Contains(dealershipId);

        public static HashSet<int> GetScopedSubdealerUserIds(
            IEnumerable<UserOrgRole> orgRoles,
            HashSet<int>? dealershipFilter,
            int? subdealerRoleId = null)
        {
            var query = orgRoles.Where(a => a.IsActive);
            if (subdealerRoleId.HasValue)
                query = query.Where(a => a.RoleId == subdealerRoleId.Value);

            if (dealershipFilter != null)
            {
                query = query.Where(a =>
                    a.DealershipId.HasValue && dealershipFilter.Contains(a.DealershipId.Value));
            }

            return query.Select(a => a.UserId).ToHashSet();
        }

        public static bool IsSubdealerInScope(int subdealerUserId, HashSet<int>? scopedSubdealerUserIds)
            => scopedSubdealerUserIds == null || scopedSubdealerUserIds.Contains(subdealerUserId);
    }
}
