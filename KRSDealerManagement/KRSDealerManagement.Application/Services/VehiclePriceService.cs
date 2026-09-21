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

            var delta = catalogPrice.Value - vehicle.CurrentPrice;
            var auditReason = delta > 0
                ? $"Price increased for chassis {vehicle.ChassisNumber} on {invoice:yyyy-MM-dd}"
                : $"Price decreased for chassis {vehicle.ChassisNumber} on {invoice:yyyy-MM-dd}";

            var (applied, _) = await ApplyVehiclePriceChangeAsync(
                vehicle,
                catalogPrice.Value,
                invoice,
                changedBy,
                reasonLabel: "invoiced",
                auditReason: auditReason,
                requireAccountAdjustment: true);
            return applied;
        }

        public async Task<PriceIncreaseApplyResult> TryApplyCatalogPriceIncreaseForMasterAsync(
            int vehicleMasterId,
            DateTime asOfDate,
            int changedBy)
        {
            var master = await _unitOfWork.VehicleMasters.GetByIdAsync(vehicleMasterId);
            if (master == null)
            {
                return new PriceIncreaseApplyResult
                {
                    Status = PriceUpdateLineStatuses.Error,
                    Message = "Dealer stock vehicle not found."
                };
            }

            if (master.WarrantyOnly)
            {
                return new PriceIncreaseApplyResult
                {
                    Status = PriceUpdateLineStatuses.Skipped,
                    Message = "Warranty-only dealer stock is excluded."
                };
            }

            if (master.IsAllocated)
            {
                return new PriceIncreaseApplyResult
                {
                    Status = PriceUpdateLineStatuses.Skipped,
                    Message = "Vehicle is already allocated to a subdealer."
                };
            }

            var asOf = asOfDate.Date;
            var catalogToday = await GetPriceAsOfAsync(master.ModelId, master.ColorId, asOf);
            if (!catalogToday.HasValue)
            {
                return new PriceIncreaseApplyResult
                {
                    Status = PriceUpdateLineStatuses.Skipped,
                    Message = $"No catalogue price for {asOf:yyyy-MM-dd}."
                };
            }

            var catalogYesterday = await GetPriceAsOfAsync(master.ModelId, master.ColorId, asOf.AddDays(-1));
            if (!catalogYesterday.HasValue)
            {
                return new PriceIncreaseApplyResult
                {
                    Status = PriceUpdateLineStatuses.Skipped,
                    Message = $"No catalogue price for {(asOf.AddDays(-1)):yyyy-MM-dd}."
                };
            }

            if (catalogToday.Value <= catalogYesterday.Value)
            {
                return new PriceIncreaseApplyResult
                {
                    Status = PriceUpdateLineStatuses.Skipped,
                    Message = catalogToday.Value == catalogYesterday.Value
                        ? "Catalogue price unchanged."
                        : "Catalogue price is not higher than previous day.",
                    OldPrice = catalogYesterday.Value,
                    NewPrice = catalogToday.Value
                };
            }

            return new PriceIncreaseApplyResult
            {
                Applied = true,
                Status = PriceUpdateLineStatuses.Updated,
                Message = "Catalogue price increased for dealer stock (ready to allocate).",
                OldPrice = catalogYesterday.Value,
                NewPrice = catalogToday.Value,
                Delta = catalogToday.Value - catalogYesterday.Value,
                TransactionLogged = false
            };
        }

        public async Task<PriceIncreaseApplyResult> TryApplyCatalogPriceIncreaseAsync(int vehicleId, DateTime asOfDate, int changedBy)
        {
            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(vehicleId);
            if (vehicle == null)
            {
                return new PriceIncreaseApplyResult
                {
                    Status = PriceUpdateLineStatuses.Error,
                    Message = "Vehicle not found."
                };
            }

            if (!vehicle.SubdealerId.HasValue)
            {
                return new PriceIncreaseApplyResult
                {
                    Status = PriceUpdateLineStatuses.Skipped,
                    Message = "Vehicle is not allocated to a subdealer.",
                    OldPrice = vehicle.CurrentPrice,
                    NewPrice = vehicle.CurrentPrice
                };
            }

            var asOf = asOfDate.Date;
            var catalogPrice = await GetPriceAsOfAsync(vehicle.ModelId, vehicle.ColorId, asOf);
            if (!catalogPrice.HasValue)
            {
                return new PriceIncreaseApplyResult
                {
                    Status = PriceUpdateLineStatuses.Skipped,
                    Message = $"No catalogue price for {asOf:yyyy-MM-dd}.",
                    OldPrice = vehicle.CurrentPrice,
                    NewPrice = vehicle.CurrentPrice
                };
            }

            if (catalogPrice.Value <= vehicle.CurrentPrice)
            {
                return new PriceIncreaseApplyResult
                {
                    Status = PriceUpdateLineStatuses.Skipped,
                    Message = catalogPrice.Value == vehicle.CurrentPrice
                        ? "Already at catalogue price."
                        : "Catalogue price is not higher than current price.",
                    OldPrice = vehicle.CurrentPrice,
                    NewPrice = vehicle.CurrentPrice
                };
            }

            var auditReason = $"Price increased for chassis {vehicle.ChassisNumber} on {asOf:yyyy-MM-dd}";
            var oldPrice = vehicle.CurrentPrice;
            try
            {
                var (applied, accountId) = await ApplyVehiclePriceChangeAsync(
                    vehicle,
                    catalogPrice.Value,
                    asOf,
                    changedBy,
                    reasonLabel: $"scheduled {asOf:yyyy-MM-dd}",
                    auditReason: auditReason,
                    requireAccountAdjustment: true);

                return new PriceIncreaseApplyResult
                {
                    Applied = applied,
                    Status = applied ? PriceUpdateLineStatuses.Updated : PriceUpdateLineStatuses.Skipped,
                    Message = applied ? "Price increased and account debited." : "No change applied.",
                    OldPrice = oldPrice,
                    NewPrice = applied ? catalogPrice.Value : oldPrice,
                    Delta = applied ? catalogPrice.Value - oldPrice : 0,
                    AccountId = accountId,
                    TransactionLogged = applied
                };
            }
            catch (Exception ex)
            {
                return new PriceIncreaseApplyResult
                {
                    Status = PriceUpdateLineStatuses.Error,
                    Message = ex.Message,
                    OldPrice = oldPrice,
                    NewPrice = catalogPrice.Value,
                    Delta = catalogPrice.Value - oldPrice
                };
            }
        }

        public async Task ApplyCatalogPriceRevisionAsync(int modelId, int colorId, decimal newPrice, DateTime effectiveFrom, int changedBy)
        {
            var effective = effectiveFrom.Date;
            var warrantyOnlyVehicleIds = await WarrantyOnlyVehicleFlowHelper.GetWarrantyOnlyVehicleIdsAsync(_unitOfWork);
            var vehicles = VehicleLifecycleHelper.FilterActiveLifecycle(await _unitOfWork.Vehicles.GetAllAsync())
                .Where(v => v.ModelId == modelId && v.ColorId == colorId && v.SubdealerId.HasValue)
                .Where(v => !warrantyOnlyVehicleIds.Contains(v.VehicleId))
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

        private async Task<(bool Applied, int? AccountId)> ApplyVehiclePriceChangeAsync(
            Vehicle vehicle,
            decimal newPrice,
            DateTime referenceDate,
            int changedBy,
            string reasonLabel,
            string auditReason,
            bool requireAccountAdjustment = false)
        {
            var oldPrice = vehicle.CurrentPrice;
            if (oldPrice == newPrice) return (false, null);

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
                return (true, null);
            }

            var accounts = (await _unitOfWork.SubdealerAccounts.GetAllAsync()).ToList();
            var account = accounts.FirstOrDefault(a => a.SubdealerId == vehicle.SubdealerId.Value && a.IsActive);
            if (account == null)
            {
                if (requireAccountAdjustment)
                    throw new InvalidOperationException("No active account found for the subdealer.");
                return (true, null);
            }

            var balances = (await _unitOfWork.AccountBalances.GetAllAsync()).ToList();
            var balance = balances.FirstOrDefault(b => b.SubdealerAccountId == account.AccountId);
            if (balance == null)
            {
                if (requireAccountAdjustment)
                    throw new InvalidOperationException("Account balance record not found.");
                return (true, account.AccountId);
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

            return (true, account.AccountId);
        }
    }
}
