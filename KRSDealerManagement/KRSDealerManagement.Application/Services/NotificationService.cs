using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.Services
{
    public static class NotificationService
    {
        public static async Task<IReadOnlyList<int>> ResolveTargetDealershipIdsAsync(
            IUnitOfWork unitOfWork,
            bool targetAllLocations,
            IReadOnlyList<int> dealershipIds)
        {
            if (targetAllLocations)
            {
                return (await unitOfWork.Dealerships.GetAllAsync())
                    .Where(d => d.IsActive)
                    .Select(d => d.DealershipId)
                    .OrderBy(id => id)
                    .ToList();
            }

            return dealershipIds
                .Where(id => id > 0)
                .Distinct()
                .OrderBy(id => id)
                .ToList();
        }

        public static async Task<IReadOnlyList<UserOrgRole>> ResolveSubdealerRecipientsAsync(
            IUnitOfWork unitOfWork,
            IReadOnlyList<int> dealershipIds)
        {
            if (dealershipIds.Count == 0)
                return Array.Empty<UserOrgRole>();

            var roles = await unitOfWork.Roles.GetAllAsync();
            var subRole = roles.FirstOrDefault(r =>
                r.RoleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase));
            if (subRole == null)
                return Array.Empty<UserOrgRole>();

            var activeUserIds = (await unitOfWork.Users.GetAllAsync())
                .Where(u => u.IsActive)
                .Select(u => u.UserId)
                .ToHashSet();

            var dealershipSet = dealershipIds.ToHashSet();

            return (await unitOfWork.UserOrgRoles.GetAllAsync())
                .Where(a => a.IsActive
                    && a.SubDealerId.HasValue
                    && a.DealershipId.HasValue
                    && a.RoleId == subRole.RoleId
                    && dealershipSet.Contains(a.DealershipId.Value)
                    && activeUserIds.Contains(a.UserId))
                .GroupBy(a => a.UserId)
                .Select(g => g.OrderByDescending(a => a.IsPrimary).ThenBy(a => a.UserOrgRoleId).First())
                .OrderBy(a => a.UserId)
                .ToList();
        }

        public static async Task ReplaceTargetsAsync(
            IUnitOfWork unitOfWork,
            int notificationId,
            IReadOnlyList<int> dealershipIds)
        {
            var existing = (await unitOfWork.NotificationTargets.GetAllAsync())
                .Where(t => t.NotificationId == notificationId)
                .ToList();

            foreach (var row in existing)
                await unitOfWork.NotificationTargets.DeleteAsync(row.NotificationTargetId);

            foreach (var dealershipId in dealershipIds)
            {
                await unitOfWork.NotificationTargets.AddAsync(new NotificationTarget
                {
                    NotificationId = notificationId,
                    DealershipId = dealershipId
                });
            }
        }

        public static async Task SyncRecipientsAsync(
            IUnitOfWork unitOfWork,
            int notificationId,
            IReadOnlyList<int> dealershipIds)
        {
            var desired = await ResolveSubdealerRecipientsAsync(unitOfWork, dealershipIds);
            var desiredUserIds = desired.Select(a => a.UserId).ToHashSet();

            var existing = (await unitOfWork.NotificationRecipients.GetAllAsync())
                .Where(r => r.NotificationId == notificationId)
                .ToList();

            foreach (var row in existing.Where(r => !desiredUserIds.Contains(r.UserId)))
                await unitOfWork.NotificationRecipients.DeleteAsync(row.NotificationRecipientId);

            var existingUserIds = existing.Select(r => r.UserId).ToHashSet();
            foreach (var assignment in desired.Where(a => !existingUserIds.Contains(a.UserId)))
            {
                await unitOfWork.NotificationRecipients.AddAsync(new NotificationRecipient
                {
                    NotificationId = notificationId,
                    UserId = assignment.UserId,
                    SubDealerId = assignment.SubDealerId!.Value,
                    DealershipId = assignment.DealershipId!.Value,
                    IsRead = false,
                    ReadDate = null
                });
            }
        }

        public static string FormatLocationSummary(
            bool targetAllLocations,
            IReadOnlyList<string> dealershipNames)
        {
            if (targetAllLocations)
                return "All locations";

            if (dealershipNames.Count == 0)
                return "—";

            return string.Join(", ", dealershipNames);
        }
    }
}
