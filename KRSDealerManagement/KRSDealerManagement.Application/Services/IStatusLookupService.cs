using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;

namespace KRSDealerManagement.Application.Services
{
    public interface IStatusLookupService
    {
        Task<IReadOnlyList<StatusLookup>> GetActiveByCategoryAsync(string category);
        Task<IReadOnlyList<StatusLookup>> GetAllByCategoryAsync(string? category = null);
        Task<IReadOnlyDictionary<int, StatusLookup>> GetMapAsync(string category);
        Task<string> GetNameAsync(string category, int statusValue, string fallback = "Unknown");
        Task<string> GetBadgeClassAsync(string category, int statusValue, string fallback = "bg-secondary");
        void InvalidateCache();
    }

    public class StatusLookupService : IStatusLookupService
    {
        private readonly IUnitOfWork _unitOfWork;
        private IReadOnlyList<StatusLookup>? _cache;

        public StatusLookupService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private async Task<IReadOnlyList<StatusLookup>> LoadAsync()
        {
            if (_cache != null) return _cache;
            _cache = (await _unitOfWork.StatusLookups.GetAllAsync())
                .Where(s => s.IsActive)
                .OrderBy(s => s.Category)
                .ThenBy(s => s.SortOrder)
                .ToList();
            return _cache;
        }

        public void InvalidateCache() => _cache = null;

        public async Task<IReadOnlyList<StatusLookup>> GetActiveByCategoryAsync(string category)
        {
            var all = await LoadAsync();
            return all
                .Where(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public async Task<IReadOnlyList<StatusLookup>> GetAllByCategoryAsync(string? category = null)
        {
            var rows = (await _unitOfWork.StatusLookups.GetAllAsync()).AsEnumerable();
            if (!string.IsNullOrWhiteSpace(category))
                rows = rows.Where(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            return rows
                .OrderBy(s => s.Category)
                .ThenBy(s => s.SortOrder)
                .ThenBy(s => s.StatusValue)
                .ToList();
        }

        public async Task<IReadOnlyDictionary<int, StatusLookup>> GetMapAsync(string category)
        {
            var list = await GetAllByCategoryAsync(category);
            return list.ToDictionary(s => s.StatusValue);
        }

        public async Task<string> GetNameAsync(string category, int statusValue, string fallback = "Unknown")
        {
            var map = await GetMapAsync(category);
            return map.TryGetValue(statusValue, out var s) ? s.StatusName : fallback;
        }

        public async Task<string> GetBadgeClassAsync(string category, int statusValue, string fallback = "bg-secondary")
        {
            var map = await GetMapAsync(category);
            return map.TryGetValue(statusValue, out var s) && !string.IsNullOrWhiteSpace(s.BadgeClass)
                ? s.BadgeClass
                : fallback;
        }
    }
}
