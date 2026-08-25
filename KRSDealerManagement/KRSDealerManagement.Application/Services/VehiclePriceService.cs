using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;

namespace KRSDealerManagement.Application.Services
{
    public class VehiclePriceService : IVehiclePriceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public VehiclePriceService(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<decimal?> GetPriceAsOfAsync(int modelId, int colorId, DateTime asOfDate)
        {
            var asOf = asOfDate.Date;
            var prices = await _unitOfWork.VehiclePriceHistories.GetAllAsync();
            var match = VehiclePriceCoverageHelper.FindActivePrice(prices, modelId, colorId, asOf);
            return match?.Price;
        }

        public async Task<string?> ValidatePriceForVehicleCreateAsync(int modelId, int colorId, DateTime asOfDate)
        {
            var prices = await _unitOfWork.VehiclePriceHistories.GetAllAsync();
            var model = (await _unitOfWork.VehicleModels.GetAllAsync()).FirstOrDefault(m => m.ModelId == modelId);
            var color = (await _unitOfWork.VehicleColors.GetAllAsync()).FirstOrDefault(c => c.ColorId == colorId);

            return VehiclePriceCoverageHelper.ValidateForDate(
                prices,
                modelId,
                colorId,
                asOfDate,
                model?.ModelName,
                color?.ColorName);
        }

        public async Task<InvoicePriceChangePreviewDto> GetInvoicePriceChangePreviewAsync(int vehicleId, DateTime invoiceDate)
        {
            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(vehicleId);
            if (vehicle == null)
            {
                return new InvoicePriceChangePreviewDto
                {
                    ErrorMessage = "Vehicle not found."
                };
            }

            var invoice = invoiceDate.Date;
            var catalogPrice = await GetPriceAsOfAsync(vehicle.ModelId, vehicle.ColorId, invoice);
            if (!catalogPrice.HasValue)
            {
                var validation = await ValidatePriceForVehicleCreateAsync(vehicle.ModelId, vehicle.ColorId, invoice);
                return new InvoicePriceChangePreviewDto
                {
                    CurrentVehiclePrice = vehicle.CurrentPrice,
                    HasCatalogPrice = false,
                    ErrorMessage = validation ?? $"No catalogue price found effective on {invoice:yyyy-MM-dd}."
                };
            }

            var delta = catalogPrice.Value - vehicle.CurrentPrice;
            return new InvoicePriceChangePreviewDto
            {
                CurrentVehiclePrice = vehicle.CurrentPrice,
                CatalogPrice = catalogPrice.Value,
                Delta = delta,
                WouldChange = delta != 0,
                HasCatalogPrice = true
            };
        }

        public async Task<bool> ApplyPriceOnInvoiceAsync(int vehicleId, DateTime invoiceDate, int changedBy)
        {
            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(vehicleId);
            if (vehicle == null)
                throw new InvalidOperationException("Vehicle not found.");

            if (!vehicle.SubdealerId.HasValue)
                throw new InvalidOperationException("Vehicle is not allocated to a subdealer.");

            var invoice = invoiceDate.Date;
            var catalogPrice = await GetPriceAsOfAsync(vehicle.ModelId, vehicle.ColorId, invoice);
            if (!catalogPrice.HasValue)
                throw new InvalidOperationException(
                    $"No catalogue price found for this model/color effective on {invoice:yyyy-MM-dd}.");

            return await ApplyVehiclePriceChangeAsync(
                vehicle,
                catalogPrice.Value,
                invoice,
                changedBy,
                reasonLabel: "invoiced",
                auditReason: $"Invoice price applied for chassis {vehicle.ChassisNumber} on {invoice:yyyy-MM-dd}",
                requireAccountAdjustment: true);
        }

        public async Task ApplyCatalogPriceRevisionAsync(int modelId, int colorId, decimal newPrice, DateTime effectiveFrom, int changedBy)
        {
            var effective = effectiveFrom.Date;
            var vehicles = (await _unitOfWork.Vehicles.GetAllAsync())
                .Where(v => v.ModelId == modelId && v.ColorId == colorId && v.SubdealerId.HasValue)
                .ToList();
            if (vehicles.Count == 0) return;

            var bookings = (await _unitOfWork.VehicleBookings.GetAllAsync())
                .GroupBy(b => b.VehicleId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(b => b.SubmittedDate).First());

            foreach (var vehicle in vehicles)
            {
                bookings.TryGetValue(vehicle.VehicleId, out var booking);
                var referenceDate = booking?.InvoiceDate?.Date ?? vehicle.CreatedDate.Date;
                if (referenceDate < effective) continue;

                await ApplyVehiclePriceChangeAsync(
                    vehicle,
                    newPrice,
                    effective,
                    changedBy,
                    reasonLabel: $"effective {effective:yyyy-MM-dd}",
                    auditReason: $"Price revision for chassis {vehicle.ChassisNumber} effective {effective:yyyy-MM-dd}");
            }
        }

        private async Task<bool> ApplyVehiclePriceChangeAsync(
            Vehicle vehicle,
            decimal newPrice,
            DateTime referenceDate,
            int changedBy,
            string reasonLabel,
            string auditReason,
            bool requireAccountAdjustment = false)
        {
            var oldPrice = vehicle.CurrentPrice;
            if (oldPrice == newPrice) return false;

            var delta = newPrice - oldPrice;
            var direction = delta > 0 ? "increased" : "decreased";
            var note = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Price {direction} from ₹{oldPrice:N2} to ₹{newPrice:N2} ({reasonLabel}).";
            vehicle.CurrentPrice = newPrice;
            vehicle.Notes = string.IsNullOrWhiteSpace(vehicle.Notes) ? note : $"{vehicle.Notes} {note}";
            vehicle.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.Vehicles.UpdateAsync(vehicle);

            if (!vehicle.SubdealerId.HasValue)
            {
                if (requireAccountAdjustment)
                    throw new InvalidOperationException("Vehicle is not allocated to a subdealer.");
                return true;
            }

            var accounts = (await _unitOfWork.SubdealerAccounts.GetAllAsync()).ToList();
            var account = accounts.FirstOrDefault(a => a.SubdealerId == vehicle.SubdealerId.Value && a.IsActive);
            if (account == null)
            {
                if (requireAccountAdjustment)
                    throw new InvalidOperationException("No active account found for the subdealer.");
                return true;
            }

            var balances = (await _unitOfWork.AccountBalances.GetAllAsync()).ToList();
            var balance = balances.FirstOrDefault(b => b.SubdealerAccountId == account.AccountId);
            if (balance == null)
            {
                if (requireAccountAdjustment)
                    throw new InvalidOperationException("Account balance record not found.");
                return true;
            }

            if (delta > 0)
            {
                balance.CurrentBalance -= delta;
                balance.AvailableBalance = balance.CurrentBalance - balance.ReservedAmount;
            }
            else
            {
                balance.CurrentBalance += Math.Abs(delta);
                balance.AvailableBalance = balance.CurrentBalance - balance.ReservedAmount;
            }
            balance.LastTransactionDate = DateTime.UtcNow;
            balance.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.AccountBalances.UpdateAsync(balance);

            await _auditService.LogTransactionAsync(
                accountId: account.AccountId,
                transactionType: delta > 0 ? 1 : 2,
                amount: Math.Abs(delta),
                balanceAfter: balance.CurrentBalance,
                reason: auditReason,
                referenceType: "Vehicle",
                referenceId: vehicle.VehicleId,
                remarks: note,
                initiatedBy: changedBy);

            return true;
        }
    }
}
