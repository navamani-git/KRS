using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.Helpers
{
    public sealed class CorrectionNoteLabelResolver
    {
        private const string NotSet = "Not set";

        private readonly Dictionary<int, string> _vehicleStatuses;
        private readonly Dictionary<int, string> _models;
        private readonly Dictionary<int, string> _colors;
        private readonly Dictionary<int, User> _users;
        private readonly Dictionary<int, string> _documentTypes;
        private readonly Dictionary<int, string> _rtoLocations;
        private readonly Dictionary<int, string> _financeNames;

        private CorrectionNoteLabelResolver(
            Dictionary<int, string> vehicleStatuses,
            Dictionary<int, string> models,
            Dictionary<int, string> colors,
            Dictionary<int, User> users,
            Dictionary<int, string> documentTypes,
            Dictionary<int, string> rtoLocations,
            Dictionary<int, string> financeNames)
        {
            _vehicleStatuses = vehicleStatuses;
            _models = models;
            _colors = colors;
            _users = users;
            _documentTypes = documentTypes;
            _rtoLocations = rtoLocations;
            _financeNames = financeNames;
        }

        public static async Task<CorrectionNoteLabelResolver> LoadAsync(IUnitOfWork unitOfWork)
        {
            var statusRows = (await unitOfWork.StatusLookups.GetAllAsync())
                .Where(s => s.IsActive)
                .ToList();

            var vehicleStatuses = statusRows
                .Where(s => s.Category.Equals(StatusCategories.Vehicle, StringComparison.OrdinalIgnoreCase))
                .GroupBy(s => s.StatusValue)
                .ToDictionary(g => g.Key, g => g.First().StatusName);

            var models = (await unitOfWork.VehicleModels.GetAllAsync())
                .GroupBy(m => m.ModelId)
                .ToDictionary(g => g.Key, g => g.First().ModelName);

            var colors = (await unitOfWork.VehicleColors.GetAllAsync())
                .GroupBy(c => c.ColorId)
                .ToDictionary(g => g.Key, g => g.First().ColorName);

            var users = (await unitOfWork.Users.GetAllAsync())
                .GroupBy(u => u.UserId)
                .ToDictionary(g => g.Key, g => g.First());

            var documentTypes = (await unitOfWork.DocumentTypes.GetAllAsync())
                .GroupBy(d => d.DocumentTypeId)
                .ToDictionary(g => g.Key, g => g.First().TypeName);

            var rtoLocations = (await unitOfWork.RtoLocations.GetAllAsync())
                .GroupBy(r => r.RtoLocationId)
                .ToDictionary(g => g.Key, g => g.First().LocationName);

            var financeNames = (await unitOfWork.FinanceNames.GetAllAsync())
                .GroupBy(f => f.FinanceNameId)
                .ToDictionary(g => g.Key, g => g.First().FinanceName);

            return new CorrectionNoteLabelResolver(
                vehicleStatuses,
                models,
                colors,
                users,
                documentTypes,
                rtoLocations,
                financeNames);
        }

        public string VehicleStatus(int status)
            => _vehicleStatuses.TryGetValue(status, out var name) ? name : $"Unknown status ({status})";

        public string Model(int modelId)
            => _models.TryGetValue(modelId, out var name) ? name : $"Model #{modelId}";

        public string Color(int colorId)
            => _colors.TryGetValue(colorId, out var name) ? name : $"Color #{colorId}";

        public string Subdealer(int? subdealerId)
        {
            if (!subdealerId.HasValue)
                return "Dealer showroom (unassigned)";

            return _users.TryGetValue(subdealerId.Value, out var user)
                ? user.GetFullName()
                : $"Subdealer #{subdealerId.Value}";
        }

        public string DocumentType(int documentTypeId)
            => _documentTypes.TryGetValue(documentTypeId, out var name) ? name : $"Document type #{documentTypeId}";

        public string RtoLocation(int rtoLocationId)
            => _rtoLocations.TryGetValue(rtoLocationId, out var name) ? name : $"RTO location #{rtoLocationId}";

        public string FinanceName(int financeNameId)
            => _financeNames.TryGetValue(financeNameId, out var name) ? name : $"Finance #{financeNameId}";

        public string FinanceName(int? financeNameId)
            => financeNameId.HasValue && financeNameId.Value > 0
                ? FinanceName(financeNameId.Value)
                : NotSet;

        public static string YesNo(bool value) => value ? "Yes" : "No";

        public static string DateTimeValue(DateTime? value)
            => value.HasValue ? value.Value.ToString("dd MMM yyyy, h:mm tt") : NotSet;
    }
}
