using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Helpers;

namespace KRSDealerManagement.Application.Services
{
    public interface IPriceUpdateJobService
    {
        Task<PriceUpdateRunResultDto> RunAsync(DateTime? asOfDate, string triggerSource, int? triggeredByUserId);
        Task<IReadOnlyList<PriceUpdateRunListItemDto>> GetRunsAsync();
        Task<PriceUpdateRunDetailDto?> GetRunDetailAsync(int? runId, DateTime? asOfDate, string? statusFilter);
    }

    public static class PriceUpdateTriggerSources
    {
        public const string Scheduler = "Scheduler";
        public const string Manual = "Manual";
    }

    public static class PriceUpdateLineStatuses
    {
        public const string Updated = "Updated";
        public const string Skipped = "Skipped";
        public const string Error = "Error";
    }

    public class PriceUpdateJobService : IPriceUpdateJobService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IVehiclePriceService _priceService;

        public PriceUpdateJobService(IUnitOfWork unitOfWork, IVehiclePriceService priceService)
        {
            _unitOfWork = unitOfWork;
            _priceService = priceService;
        }

        public async Task<PriceUpdateRunResultDto> RunAsync(DateTime? asOfDate, string triggerSource, int? triggeredByUserId)
        {
            var effectiveDate = (asOfDate ?? IstTime.Today).Date;
            var initiatedBy = triggeredByUserId ?? 0;

            var runId = await _unitOfWork.PriceUpdateRuns.AddAsync(new PriceUpdateRun
            {
                StartedAt = DateTime.UtcNow,
                TriggerSource = triggerSource,
                TriggeredByUserId = triggeredByUserId,
                Status = "Running",
                AsOfDate = effectiveDate
            });

            var scanned = 0;
            var updated = 0;
            var skipped = 0;
            var errors = 0;

            try
            {
                var warrantyOnlyVehicleIds = await WarrantyOnlyVehicleFlowHelper.GetWarrantyOnlyVehicleIdsAsync(_unitOfWork);

                var bookings = (await _unitOfWork.VehicleBookings.GetAllAsync())
                    .GroupBy(b => b.VehicleId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(b => b.SubmittedDate).First());

                var subdealerVehicles = VehicleLifecycleHelper.FilterActiveLifecycle(await _unitOfWork.Vehicles.GetAllAsync())
                    .Where(v => v.SubdealerId.HasValue && v.SubdealerId.Value > 0)
                    .Where(v => !UnifiedVehicleStatus.IsTerminal(v.Status))
                    .Where(v => !UnifiedVehicleStatus.IsPlaceholderChassis(v.ChassisNumber))
                    .Where(v => !warrantyOnlyVehicleIds.Contains(v.VehicleId))
                    .ToList();

                foreach (var vehicle in subdealerVehicles)
                {
                    bookings.TryGetValue(vehicle.VehicleId, out var booking);
                    if (booking?.InvoiceDate.HasValue == true)
                        continue;

                    scanned++;
                    var subdealerOutcome = await ProcessLineAsync(
                        runId,
                        () => _priceService.TryApplyCatalogPriceIncreaseAsync(vehicle.VehicleId, effectiveDate, initiatedBy),
                        vehicle.VehicleId,
                        vehicle.ChassisNumber ?? "",
                        vehicle.SubdealerId);
                    updated += subdealerOutcome.Updated;
                    skipped += subdealerOutcome.Skipped;
                    errors += subdealerOutcome.Errors;
                }

                var dealerStock = (await _unitOfWork.VehicleMasters.GetAllAsync())
                    .Where(m => !m.IsAllocated)
                    .Where(m => !m.WarrantyOnly)
                    .Where(m => !UnifiedVehicleStatus.IsPlaceholderChassis(m.ChassisNumber))
                    .ToList();

                foreach (var master in dealerStock)
                {
                    scanned++;
                    var dealerOutcome = await ProcessLineAsync(
                        runId,
                        () => _priceService.TryApplyCatalogPriceIncreaseForMasterAsync(master.VehicleMasterId, effectiveDate, initiatedBy),
                        vehicleId: 0,
                        master.ChassisNumber ?? "",
                        subDealerId: null);
                    updated += dealerOutcome.Updated;
                    skipped += dealerOutcome.Skipped;
                    errors += dealerOutcome.Errors;
                }

                var run = await _unitOfWork.PriceUpdateRuns.GetByIdAsync(runId)
                    ?? throw new InvalidOperationException("Run log not found.");
                run.CompletedAt = DateTime.UtcNow;
                run.Status = errors > 0 && updated == 0 ? "Failed" : "Completed";
                run.TotalScanned = scanned;
                run.TotalUpdated = updated;
                run.TotalSkipped = skipped;
                run.TotalErrors = errors;
                run.SummaryMessage =
                    $"Scanned {scanned}, price increased {updated}, errors {errors} (as of {effectiveDate:yyyy-MM-dd}).";
                await _unitOfWork.PriceUpdateRuns.UpdateAsync(run);

                return MapRun(run);
            }
            catch (Exception ex)
            {
                var run = await _unitOfWork.PriceUpdateRuns.GetByIdAsync(runId);
                if (run != null)
                {
                    run.CompletedAt = DateTime.UtcNow;
                    run.Status = "Failed";
                    run.TotalScanned = scanned;
                    run.TotalUpdated = updated;
                    run.TotalSkipped = skipped;
                    run.TotalErrors = errors + 1;
                    run.SummaryMessage = ex.Message;
                    await _unitOfWork.PriceUpdateRuns.UpdateAsync(run);
                }

                throw;
            }
        }

        private sealed record LineProcessOutcome(int Updated, int Skipped, int Errors);

        private async Task<LineProcessOutcome> ProcessLineAsync(
            int runId,
            Func<Task<PriceIncreaseApplyResult>> apply,
            int vehicleId,
            string chassisNumber,
            int? subDealerId)
        {
            try
            {
                var result = await apply();
                if (result.Status == PriceUpdateLineStatuses.Skipped)
                    return new LineProcessOutcome(0, 1, 0);

                await _unitOfWork.PriceUpdateRunDetails.AddAsync(new PriceUpdateRunDetail
                {
                    RunId = runId,
                    VehicleId = vehicleId,
                    ChassisNumber = chassisNumber,
                    SubDealerId = subDealerId,
                    OldPrice = result.OldPrice,
                    NewPrice = result.NewPrice,
                    Delta = result.Delta,
                    AccountId = result.AccountId,
                    TransactionLogged = result.TransactionLogged,
                    Status = result.Status,
                    Message = result.Message
                });

                if (result.Status == PriceUpdateLineStatuses.Updated)
                    return new LineProcessOutcome(1, 0, 0);
                if (result.Status == PriceUpdateLineStatuses.Error)
                    return new LineProcessOutcome(0, 0, 1);

                return new LineProcessOutcome(0, 0, 0);
            }
            catch (Exception ex)
            {
                await _unitOfWork.PriceUpdateRunDetails.AddAsync(new PriceUpdateRunDetail
                {
                    RunId = runId,
                    VehicleId = vehicleId,
                    ChassisNumber = chassisNumber,
                    SubDealerId = subDealerId,
                    Status = PriceUpdateLineStatuses.Error,
                    Message = ex.Message
                });
                return new LineProcessOutcome(0, 0, 1);
            }
        }

        public async Task<IReadOnlyList<PriceUpdateRunListItemDto>> GetRunsAsync()
        {
            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);
            return (await _unitOfWork.PriceUpdateRuns.GetAllAsync())
                .OrderByDescending(r => r.StartedAt)
                .Select(r =>
                {
                    users.TryGetValue(r.TriggeredByUserId ?? 0, out var user);
                    return new PriceUpdateRunListItemDto
                    {
                        RunId = r.RunId,
                        StartedAt = r.StartedAt,
                        CompletedAt = r.CompletedAt,
                        TriggerSource = r.TriggerSource,
                        TriggeredByName = user?.GetFullName(),
                        Status = r.Status,
                        AsOfDate = r.AsOfDate,
                        TotalScanned = r.TotalScanned,
                        TotalUpdated = r.TotalUpdated,
                        TotalSkipped = r.TotalSkipped,
                        TotalErrors = r.TotalErrors,
                        SummaryMessage = r.SummaryMessage
                    };
                })
                .ToList();
        }

        public async Task<PriceUpdateRunDetailDto?> GetRunDetailAsync(int? runId, DateTime? asOfDate, string? statusFilter)
        {
            PriceUpdateRun? run = null;
            if (asOfDate.HasValue)
            {
                var targetDate = asOfDate.Value.Date;
                run = (await _unitOfWork.PriceUpdateRuns.GetAllAsync())
                    .Where(r => r.AsOfDate.Date == targetDate)
                    .OrderByDescending(r => r.StartedAt)
                    .FirstOrDefault();
            }
            else if (runId.HasValue)
            {
                run = await _unitOfWork.PriceUpdateRuns.GetByIdAsync(runId.Value);
            }

            if (run == null) return null;

            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);
            users.TryGetValue(run.TriggeredByUserId ?? 0, out var user);

            var detailQuery = (await _unitOfWork.PriceUpdateRunDetails.GetAllAsync())
                .Where(d => d.RunId == run.RunId);

            if (!string.IsNullOrWhiteSpace(statusFilter)
                && !string.Equals(statusFilter, "All", StringComparison.OrdinalIgnoreCase))
            {
                detailQuery = detailQuery.Where(d => d.Status == statusFilter);
            }

            var lines = detailQuery
                .OrderBy(d => d.Status)
                .ThenBy(d => d.ChassisNumber)
                .Select(d => new PriceUpdateRunDetailLineDto
                {
                    ChassisNumber = d.ChassisNumber,
                    VehicleId = d.VehicleId,
                    SubDealerId = d.SubDealerId,
                    OldPrice = d.OldPrice,
                    NewPrice = d.NewPrice,
                    Delta = d.Delta,
                    AccountId = d.AccountId,
                    TransactionLogged = d.TransactionLogged,
                    Status = d.Status,
                    Message = d.Message,
                    IsDealerStock = d.VehicleId <= 0
                })
                .ToList();

            return new PriceUpdateRunDetailDto
            {
                RunId = run.RunId,
                StartedAt = run.StartedAt,
                CompletedAt = run.CompletedAt,
                TriggerSource = run.TriggerSource,
                TriggeredByName = user?.GetFullName(),
                Status = run.Status,
                AsOfDate = run.AsOfDate,
                TotalScanned = run.TotalScanned,
                TotalUpdated = run.TotalUpdated,
                TotalSkipped = run.TotalSkipped,
                TotalErrors = run.TotalErrors,
                SummaryMessage = run.SummaryMessage,
                TotalLineCount = lines.Count,
                Lines = lines
            };
        }

        private static PriceUpdateRunResultDto MapRun(PriceUpdateRun run) => new()
        {
            RunId = run.RunId,
            Status = run.Status,
            AsOfDate = run.AsOfDate,
            TotalScanned = run.TotalScanned,
            TotalUpdated = run.TotalUpdated,
            TotalSkipped = run.TotalSkipped,
            TotalErrors = run.TotalErrors,
            SummaryMessage = run.SummaryMessage,
            StartedAt = run.StartedAt,
            CompletedAt = run.CompletedAt,
            TriggerSource = run.TriggerSource
        };
    }
}
