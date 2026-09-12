using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.Services
{
    public static class StaffDealershipService
    {
        public static async Task<IReadOnlyList<int>> GetAssignedDealershipIdsAsync(IUnitOfWork unitOfWork, int userId)
        {
            var fromAssignments = (await unitOfWork.UserDealerships.GetAllAsync())
                .Where(x => x.UserId == userId && x.IsActive)
                .Select(x => x.DealershipId)
                .Distinct()
                .ToList();

            if (fromAssignments.Count > 0)
                return fromAssignments;

            var roles = (await unitOfWork.Roles.GetAllAsync()).ToDictionary(r => r.RoleId);
            return (await unitOfWork.UserOrgRoles.GetAllAsync())
                .Where(a => a.UserId == userId
                    && a.IsActive
                    && a.DealershipId.HasValue
                    && a.SubDealerId == null
                    && roles.TryGetValue(a.RoleId, out var role)
                    && !role.IsSystemRole
                    && !role.RoleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase)
                    && !role.RoleCode.Equals(RoleCodes.SystemAdmin, StringComparison.OrdinalIgnoreCase))
                .Select(a => a.DealershipId!.Value)
                .Distinct()
                .ToList();
        }

        public static async Task ReplaceUserDealershipsAsync(
            IUnitOfWork unitOfWork,
            int userId,
            IEnumerable<int> dealershipIds,
            bool isActive)
        {
            var desired = dealershipIds
                .Where(id => id > 0)
                .Distinct()
                .ToHashSet();

            var existing = (await unitOfWork.UserDealerships.GetAllAsync())
                .Where(x => x.UserId == userId)
                .ToList();

            foreach (var row in existing)
            {
                var shouldBeActive = isActive && desired.Contains(row.DealershipId);
                if (row.IsActive != shouldBeActive)
                {
                    row.IsActive = shouldBeActive;
                    row.ModifiedDate = DateTime.UtcNow;
                    await unitOfWork.UserDealerships.UpdateAsync(row);
                }

                desired.Remove(row.DealershipId);
            }

            foreach (var dealershipId in desired)
            {
                await unitOfWork.UserDealerships.AddAsync(new UserDealership
                {
                    UserId = userId,
                    DealershipId = dealershipId,
                    IsActive = isActive,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                });
            }
        }

        public static async Task ValidateStaffRoleAsync(IUnitOfWork unitOfWork, int roleId)
        {
            var role = await unitOfWork.Roles.GetByIdAsync(roleId)
                ?? throw new InvalidOperationException("Role not found.");

            if (!role.IsActive || role.IsSystemRole
                || role.RoleCode.Equals(RoleCodes.SystemAdmin, StringComparison.OrdinalIgnoreCase)
                || role.RoleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Select a valid staff role.");
        }

        public static async Task ValidateDealershipsAsync(IUnitOfWork unitOfWork, IEnumerable<int> dealershipIds)
        {
            var ids = dealershipIds.Where(id => id > 0).Distinct().ToList();
            if (ids.Count == 0)
                throw new InvalidOperationException("Select at least one location.");

            var dealerships = (await unitOfWork.Dealerships.GetAllAsync())
                .Where(d => d.IsActive)
                .Select(d => d.DealershipId)
                .ToHashSet();

            if (ids.Any(id => !dealerships.Contains(id)))
                throw new InvalidOperationException("One or more selected locations are invalid or inactive.");
        }
    }
}
