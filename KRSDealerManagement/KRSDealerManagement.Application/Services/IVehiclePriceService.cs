namespace KRSDealerManagement.Application.Services
{
    public interface IVehiclePriceService
    {
        Task<decimal?> GetPriceAsOfAsync(int modelId, int colorId, DateTime asOfDate);
        Task ApplyCatalogPriceRevisionAsync(int modelId, int colorId, decimal newPrice, DateTime effectiveFrom, int changedBy);
        /// <returns>True if vehicle price or dealer account balance was actually changed.</returns>
        Task<bool> ApplyPriceOnInvoiceAsync(int vehicleId, DateTime invoiceDate, int changedBy);
    }
}
