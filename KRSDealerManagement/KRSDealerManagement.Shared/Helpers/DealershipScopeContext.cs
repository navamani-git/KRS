using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Shared.Helpers
{
    public sealed class DealershipScopeContext
    {
        public bool IsUnrestricted { get; init; }
        public IReadOnlyList<int> AssignedDealershipIds { get; init; } = Array.Empty<int>();
        public int? ActiveFilterDealershipId { get; init; }

        public IReadOnlyList<int>? EffectiveDealershipIds
        {
            get
            {
                if (IsUnrestricted)
                    return null;

                if (ActiveFilterDealershipId.HasValue)
                    return new[] { ActiveFilterDealershipId.Value };

                return AssignedDealershipIds.Count > 0 ? AssignedDealershipIds : Array.Empty<int>();
            }
        }

        public int? LegacySingleDealershipId
        {
            get
            {
                if (IsUnrestricted)
                    return null;

                if (ActiveFilterDealershipId.HasValue)
                    return ActiveFilterDealershipId;

                return AssignedDealershipIds.Count == 1 ? AssignedDealershipIds[0] : null;
            }
        }
    }

    public static class DealershipScopeContextFactory
    {
        public static DealershipScopeContext Create(
            bool isSystemAdmin,
            IEnumerable<int>? assignedDealershipIds,
            int? activeFilterDealershipId)
        {
            if (isSystemAdmin)
            {
                return new DealershipScopeContext
                {
                    IsUnrestricted = true,
                    AssignedDealershipIds = Array.Empty<int>(),
                    ActiveFilterDealershipId = activeFilterDealershipId
                };
            }

            var assigned = assignedDealershipIds?
                .Where(id => id > 0)
                .Distinct()
                .ToList() ?? new List<int>();

            if (activeFilterDealershipId.HasValue
                && assigned.Count > 0
                && !assigned.Contains(activeFilterDealershipId.Value))
            {
                activeFilterDealershipId = null;
            }

            return new DealershipScopeContext
            {
                IsUnrestricted = false,
                AssignedDealershipIds = assigned,
                ActiveFilterDealershipId = activeFilterDealershipId
            };
        }
    }
}
