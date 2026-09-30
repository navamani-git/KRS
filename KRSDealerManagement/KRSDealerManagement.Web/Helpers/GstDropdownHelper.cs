using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace KRSDealerManagement.Web.Helpers
{
    public static class GstDropdownHelper
    {
        public static async Task<IReadOnlyList<GstRate>> GetActiveRatesAsync(IUnitOfWork unitOfWork)
        {
            return (await unitOfWork.GstRates.GetAllAsync())
                .Where(r => r.IsActive)
                .OrderBy(r => r.SortOrder)
                .ThenBy(r => r.RatePercent)
                .ToList();
        }

        /// <summary>Sets ViewBag.GstRates and ViewBag.DefaultGstRateId for _GstRateDropdown.</summary>
        public static async Task PrepareDropdownAsync(
            IUnitOfWork unitOfWork,
            Controller controller,
            string screenKey,
            int? selectedGstRateId = null)
        {
            var rates = await GetActiveRatesAsync(unitOfWork);
            controller.ViewBag.GstRates = rates;

            if (selectedGstRateId.HasValue && selectedGstRateId.Value > 0
                && rates.Any(r => r.GstRateId == selectedGstRateId.Value))
            {
                controller.ViewBag.DefaultGstRateId = selectedGstRateId.Value;
                return;
            }

            var map = await unitOfWork.GstScreenDefaults.GetRateIdByScreenKeyAsync();
            if (map.TryGetValue(screenKey, out var defaultId) && rates.Any(r => r.GstRateId == defaultId))
                controller.ViewBag.DefaultGstRateId = defaultId;
            else
                controller.ViewBag.DefaultGstRateId = rates.FirstOrDefault()?.GstRateId;
        }
    }
}
