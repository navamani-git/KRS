using KRSDealerManagement.Domain.ValueObjects;

namespace KRSDealerManagement.Domain.DomainServices
{
    /// <summary>
    /// Domain service for price calculations
    /// Handles price history tracking and fallback to previous months
    /// </summary>
    public interface IPriceCalculationService
    {
        /// <summary>
        /// Get current month's vehicle price
        /// If not available for current month, returns previous month's price
        /// </summary>
        Money GetCurrentPrice(int vehicleId);

        /// <summary>
        /// Get price for specific month and year
        /// </summary>
        Money GetPriceForMonth(int vehicleId, int month, int year);

        /// <summary>
        /// Get latest available price for vehicle
        /// </summary>
        Money GetLatestPrice(int vehicleId);

        /// <summary>
        /// Check if vehicle has price for current month
        /// </summary>
        bool HasPriceForCurrentMonth(int vehicleId);

        /// <summary>
        /// Calculate total cost for purchase order
        /// Includes price × quantity
        /// </summary>
        Money CalculateTotalCost(int vehicleId, int quantity);
    }
}
