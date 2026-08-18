using KRSDealerManagement.Domain.Repositories;

namespace KRSDealerManagement.Application.Helpers
{
    public static class ModelColorValidation
    {
        public static async Task EnsureMappedAsync(IUnitOfWork unitOfWork, int modelId, int colorId)
        {
            if (!await unitOfWork.VehicleModelColors.IsMappedAsync(modelId, colorId))
                throw new InvalidOperationException("Selected color is not available for this vehicle model.");
        }

        public static async Task EnsureColorsExistAndActiveAsync(IUnitOfWork unitOfWork, IReadOnlyList<int> colorIds)
        {
            if (colorIds == null || colorIds.Count == 0)
                throw new InvalidOperationException("At least one color must be selected for the model.");

            var colors = await unitOfWork.VehicleColors.GetAllAsync();
            var activeIds = colors.Where(c => c.IsActive).Select(c => c.ColorId).ToHashSet();

            foreach (var colorId in colorIds.Distinct())
            {
                if (!activeIds.Contains(colorId))
                    throw new InvalidOperationException($"Color #{colorId} is not valid or inactive.");
            }
        }
    }
}
