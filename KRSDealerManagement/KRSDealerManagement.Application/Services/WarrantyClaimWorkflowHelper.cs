using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.Services
{
    public static class WarrantyClaimWorkflowHelper
    {
        public static async Task RecordHistoryAsync(
            IUnitOfWork unitOfWork,
            int claimId,
            int? fromStatus,
            int toStatus,
            int userId,
            string? notes)
        {
            await unitOfWork.WarrantyClaimStatusHistories.AddAsync(new WarrantyClaimStatusHistory
            {
                WarrantyClaimId = claimId,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                ChangedByUserId = userId,
                ChangedDate = DateTime.UtcNow,
                Notes = notes
            });
        }

        public static async Task<string> GenerateClaimNumberAsync(IUnitOfWork unitOfWork)
        {
            var prefix = $"WC-{DateTime.UtcNow:yyyyMMdd}-";
            var count = (await unitOfWork.WarrantyClaims.GetAllAsync())
                .Count(c => c.ClaimNumber.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            return $"{prefix}{(count + 1):D4}";
        }

        public static void ValidateRequiredAttachments(string claimType, IReadOnlyDictionary<string, string> paths)
        {
            var required = string.Equals(claimType, WarrantyClaimTypes.Campaign, StringComparison.OrdinalIgnoreCase)
                ? WarrantyAttachmentTypes.RequiredForCampaign
                : WarrantyAttachmentTypes.RequiredForWarranty;

            var missing = required.Where(t => !paths.ContainsKey(t) || string.IsNullOrWhiteSpace(paths[t])).ToList();
            if (missing.Count > 0)
            {
                var names = string.Join(", ", missing.Select(WarrantyAttachmentTypes.GetDisplayName));
                throw new InvalidOperationException($"Missing required attachments: {names}");
            }
        }
    }
}
