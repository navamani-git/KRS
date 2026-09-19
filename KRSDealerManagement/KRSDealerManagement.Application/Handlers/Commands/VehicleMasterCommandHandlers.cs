using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class CreateVehicleMasterCommandHandler : IRequestHandler<CreateVehicleMasterCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateVehicleMasterCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<int> Handle(CreateVehicleMasterCommand request, CancellationToken cancellationToken)
        {
            await ModelColorValidation.EnsureMappedAsync(_unitOfWork, request.ModelId, request.ColorId);

            var chassis = request.ChassisNumber.Trim().ToUpperInvariant();
            await GlobalUniqueValidation.EnsureChassisAvailableAsync(_unitOfWork, chassis);

            var master = new VehicleMaster
            {
                DealershipId = request.DealershipId,
                ChassisNumber = chassis,
                ModelId = request.ModelId,
                ColorId = request.ColorId,
                MotorNo = request.MotorNo.Trim(),
                BatteryNo = request.BatteryNo.Trim(),
                ChargerNo = request.ChargerNo.Trim(),
                ControllerNo = request.ControllerNo.Trim(),
                ConverterNo = request.ConverterNo.Trim(),
                AmpereInvoiceNo = request.AmpereInvoiceNo.Trim(),
                AmpereInvoiceDate = request.AmpereInvoiceDate.Date,
                ReceivedDate = request.ReceivedDate.Date,
                IsAllocated = false,
                Remarks = request.Remarks,
                CreatedBy = request.CreatedBy,
                CreatedDate = DateTime.UtcNow,
                ModifiedBy = request.CreatedBy,
                ModifiedDate = DateTime.UtcNow
            };

            var id = await _unitOfWork.VehicleMasters.AddAsync(master);
            await _unitOfWork.VehicleMasters.AddHistoryAsync(new VehicleMasterHistory
            {
                VehicleMasterId = id,
                Action = "Created",
                Remarks = request.Remarks,
                UserId = request.CreatedBy
            });
            await _unitOfWork.SaveChangesAsync();
            return id;
        }
    }

    public class UpdateVehicleMasterCommandHandler : IRequestHandler<UpdateVehicleMasterCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateVehicleMasterCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<bool> Handle(UpdateVehicleMasterCommand request, CancellationToken cancellationToken)
        {
            var master = await _unitOfWork.VehicleMasters.GetByIdAsync(request.VehicleMasterId)
                ?? throw new InvalidOperationException("Vehicle master record not found.");
            if (master.IsAllocated)
                throw new InvalidOperationException("Allocated vehicles cannot be edited.");
            if (master.WarrantyOnly)
                throw new InvalidOperationException("Warranty-only vehicles must be edited from Warranty-Only Vehicles.");

            await ModelColorValidation.EnsureMappedAsync(_unitOfWork, request.ModelId, request.ColorId);

            master.ModelId = request.ModelId;
            master.ColorId = request.ColorId;
            master.MotorNo = request.MotorNo.Trim();
            master.BatteryNo = request.BatteryNo.Trim();
            master.ChargerNo = request.ChargerNo.Trim();
            master.ControllerNo = request.ControllerNo.Trim();
            master.ConverterNo = request.ConverterNo.Trim();
            master.AmpereInvoiceNo = request.AmpereInvoiceNo.Trim();
            master.AmpereInvoiceDate = request.AmpereInvoiceDate;
            master.ReceivedDate = request.ReceivedDate;
            master.Remarks = request.Remarks;
            master.ModifiedBy = request.ModifiedBy;
            master.ModifiedDate = DateTime.UtcNow;

            await _unitOfWork.VehicleMasters.UpdateAsync(master);
            await _unitOfWork.VehicleMasters.AddHistoryAsync(new VehicleMasterHistory
            {
                VehicleMasterId = master.VehicleMasterId,
                Action = "Edited",
                Remarks = request.Remarks,
                UserId = request.ModifiedBy
            });
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }

    public class DeleteVehicleMasterCommandHandler : IRequestHandler<DeleteVehicleMasterCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeleteVehicleMasterCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<bool> Handle(DeleteVehicleMasterCommand request, CancellationToken cancellationToken)
        {
            var master = await _unitOfWork.VehicleMasters.GetByIdAsync(request.VehicleMasterId)
                ?? throw new InvalidOperationException("Vehicle master record not found.");
            if (master.IsAllocated)
                throw new InvalidOperationException("Allocated vehicles cannot be deleted.");
            if (master.WarrantyOnly)
                throw new InvalidOperationException("Warranty-only vehicles must be managed from Warranty-Only Vehicles.");

            await _unitOfWork.VehicleMasters.AddHistoryAsync(new VehicleMasterHistory
            {
                VehicleMasterId = master.VehicleMasterId,
                Action = "Deleted",
                Remarks = request.Remarks,
                UserId = request.DeletedBy
            });
            await _unitOfWork.VehicleMasters.DeleteAsync(request.VehicleMasterId);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }

    public class ImportVehicleMastersCommandHandler : IRequestHandler<ImportVehicleMastersCommand, ImportVehicleMastersResult>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ImportVehicleMastersCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<ImportVehicleMastersResult> Handle(ImportVehicleMastersCommand request, CancellationToken cancellationToken)
        {
            var result = new ImportVehicleMastersResult();
            if (request.Rows == null || request.Rows.Count == 0)
            {
                result.Errors.Add("No rows to import.");
                return result;
            }

            var models = (await _unitOfWork.VehicleModels.GetAllAsync())
                .Where(m => m.IsActive)
                .ToDictionary(m => m.ModelId);
            var modelsByName = models.Values
                .GroupBy(m => m.ModelName.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var colors = (await _unitOfWork.VehicleColors.GetAllAsync())
                .Where(c => c.IsActive)
                .ToDictionary(c => c.ColorId);
            var colorsByName = colors.Values
                .GroupBy(c => c.ColorName.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var fileChassis = request.Rows
                .Select(r => r.ChassisNumber?.Trim().ToUpperInvariant() ?? "")
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();
            var dupInFile = fileChassis.GroupBy(c => c).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (dupInFile.Count > 0)
            {
                result.Errors.Add($"Duplicate chassis in file: {string.Join(", ", dupInFile)}");
                return result;
            }

            for (int i = 0; i < request.Rows.Count; i++)
            {
                var row = request.Rows[i];
                var line = i + 2;
                var chassis = row.ChassisNumber?.Trim().ToUpperInvariant() ?? "";
                var dealershipId = row.DealershipId > 0 ? row.DealershipId : request.DealershipId;
                if (dealershipId <= 0)
                {
                    result.Errors.Add($"Row {line}: dealership is required.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(chassis)
                    || string.IsNullOrWhiteSpace(row.MotorNo)
                    || string.IsNullOrWhiteSpace(row.BatteryNo)
                    || string.IsNullOrWhiteSpace(row.ChargerNo)
                    || string.IsNullOrWhiteSpace(row.ControllerNo)
                    || string.IsNullOrWhiteSpace(row.ConverterNo)
                    || string.IsNullOrWhiteSpace(row.AmpereInvoiceNo)
                    || row.AmpereInvoiceDate == default
                    || row.ReceivedDate == default)
                {
                    result.Errors.Add($"Row {line}: all columns are mandatory.");
                    continue;
                }

                if (!TryResolveModel(row, models, modelsByName, out var model, out var modelError))
                {
                    result.Errors.Add($"Row {line}: {modelError}");
                    continue;
                }
                if (!TryResolveColor(row, colors, colorsByName, out var color, out var colorError))
                {
                    result.Errors.Add($"Row {line}: {colorError}");
                    continue;
                }

                try
                {
                    await GlobalUniqueValidation.EnsureChassisAvailableAsync(_unitOfWork, chassis);
                }
                catch (InvalidOperationException ex)
                {
                    result.Errors.Add($"Row {line}: {ex.Message}");
                    continue;
                }

                try
                {
                    await ModelColorValidation.EnsureMappedAsync(_unitOfWork, model.ModelId, color.ColorId);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Row {line}: {ex.Message}");
                    continue;
                }
            }

            if (result.Errors.Count > 0)
                return result;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var row in request.Rows)
                {
                    TryResolveModel(row, models, modelsByName, out var model, out _);
                    TryResolveColor(row, colors, colorsByName, out var color, out _);
                    var chassis = row.ChassisNumber.Trim().ToUpperInvariant();
                    var dealershipId = row.DealershipId > 0 ? row.DealershipId : request.DealershipId;

                    var id = await _unitOfWork.VehicleMasters.AddAsync(new VehicleMaster
                    {
                        DealershipId = dealershipId,
                        ChassisNumber = chassis,
                        ModelId = model.ModelId,
                        ColorId = color.ColorId,
                        MotorNo = row.MotorNo.Trim(),
                        BatteryNo = row.BatteryNo.Trim(),
                        ChargerNo = row.ChargerNo.Trim(),
                        ControllerNo = row.ControllerNo.Trim(),
                        ConverterNo = row.ConverterNo.Trim(),
                        AmpereInvoiceNo = row.AmpereInvoiceNo.Trim(),
                        AmpereInvoiceDate = row.AmpereInvoiceDate.Date,
                        ReceivedDate = row.ReceivedDate.Date,
                        IsAllocated = false,
                        Remarks = row.Remarks,
                        CreatedBy = request.ImportedBy,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedBy = request.ImportedBy,
                        ModifiedDate = DateTime.UtcNow
                    });

                    await _unitOfWork.VehicleMasters.AddHistoryAsync(new VehicleMasterHistory
                    {
                        VehicleMasterId = id,
                        Action = "Imported",
                        Remarks = row.Remarks,
                        UserId = request.ImportedBy
                    });
                    result.ImportedCount++;
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            return result;
        }

        public static bool TryResolveModel(
            ImportVehicleMasterRow row,
            IReadOnlyDictionary<int, Domain.Entities.VehicleModel> modelsById,
            IReadOnlyDictionary<string, Domain.Entities.VehicleModel> modelsByName,
            out Domain.Entities.VehicleModel model,
            out string error)
        {
            if (row.ModelId is > 0 && modelsById.TryGetValue(row.ModelId.Value, out model!))
            {
                error = "";
                return true;
            }

            if (!string.IsNullOrWhiteSpace(row.ModelName)
                && modelsByName.TryGetValue(row.ModelName.Trim(), out model!))
            {
                error = "";
                return true;
            }

            model = null!;
            error = row.ModelId is > 0
                ? $"unknown model id '{row.ModelId}'."
                : "ModelId is required (see Lookups sheet).";
            return false;
        }

        public static bool TryResolveColor(
            ImportVehicleMasterRow row,
            IReadOnlyDictionary<int, Domain.Entities.VehicleColor> colorsById,
            IReadOnlyDictionary<string, Domain.Entities.VehicleColor> colorsByName,
            out Domain.Entities.VehicleColor color,
            out string error)
        {
            if (row.ColorId is > 0 && colorsById.TryGetValue(row.ColorId.Value, out color!))
            {
                error = "";
                return true;
            }

            if (!string.IsNullOrWhiteSpace(row.ColorName)
                && colorsByName.TryGetValue(row.ColorName.Trim(), out color!))
            {
                error = "";
                return true;
            }

            color = null!;
            error = row.ColorId is > 0
                ? $"unknown color id '{row.ColorId}'."
                : "ColorId is required (see Lookups sheet).";
            return false;
        }
    }

    public class TransferVehicleMasterCommandHandler : IRequestHandler<TransferVehicleMasterCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;

        public TransferVehicleMasterCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<bool> Handle(TransferVehicleMasterCommand request, CancellationToken cancellationToken)
        {
            var master = await _unitOfWork.VehicleMasters.GetByIdAsync(request.VehicleMasterId)
                ?? throw new InvalidOperationException("Vehicle master record not found.");

            if (master.IsAllocated)
                throw new InvalidOperationException("Allocated vehicles cannot be transferred between branches.");
            if (master.WarrantyOnly)
                throw new InvalidOperationException("Warranty-only vehicles cannot be transferred.");

            if (master.DealershipId == request.TargetDealershipId)
                throw new InvalidOperationException("Vehicle is already at the selected branch.");

            var dealerships = (await _unitOfWork.Dealerships.GetAllAsync()).ToDictionary(d => d.DealershipId);
            if (!dealerships.TryGetValue(request.TargetDealershipId, out var targetDealership) || !targetDealership.IsActive)
                throw new InvalidOperationException("Target branch was not found or is inactive.");

            dealerships.TryGetValue(master.DealershipId, out var sourceDealership);
            var fromName = sourceDealership?.DealershipName ?? $"Branch #{master.DealershipId}";
            var toName = targetDealership.DealershipName;
            var transferNote = $"Transferred from {fromName} to {toName}";
            if (!string.IsNullOrWhiteSpace(request.Remarks))
                transferNote = $"{transferNote}. {request.Remarks.Trim()}";

            master.DealershipId = request.TargetDealershipId;
            master.ModifiedBy = request.TransferredBy;
            master.ModifiedDate = DateTime.UtcNow;

            await _unitOfWork.VehicleMasters.UpdateAsync(master);
            await _unitOfWork.VehicleMasters.AddHistoryAsync(new VehicleMasterHistory
            {
                VehicleMasterId = master.VehicleMasterId,
                Action = "BranchTransfer",
                Remarks = transferNote,
                UserId = request.TransferredBy
            });
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }

    public class CreateWarrantyOnlyVehicleMasterCommandHandler : IRequestHandler<CreateWarrantyOnlyVehicleMasterCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateWarrantyOnlyVehicleMasterCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<int> Handle(CreateWarrantyOnlyVehicleMasterCommand request, CancellationToken cancellationToken)
        {
            await WarrantyOnlyVehicleFlowHelper.EnsureOwnShowroomExistsAsync(_unitOfWork, request.DealershipId);
            var (_, subdealerUserId) = await WarrantyOnlyVehicleFlowHelper.ResolveOwnShowroomAsync(
                _unitOfWork, request.DealershipId, request.SubDealerId);

            await ModelColorValidation.EnsureMappedAsync(_unitOfWork, request.ModelId, request.ColorId);

            var chassis = request.ChassisNumber.Trim().ToUpperInvariant();
            await GlobalUniqueValidation.EnsureChassisAvailableAsync(_unitOfWork, chassis);

            var today = DateTime.UtcNow.Date;
            var master = new VehicleMaster
            {
                DealershipId = request.DealershipId,
                ChassisNumber = chassis,
                ModelId = request.ModelId,
                ColorId = request.ColorId,
                MotorNo = "-",
                BatteryNo = "-",
                ChargerNo = "-",
                ControllerNo = "-",
                ConverterNo = "-",
                AmpereInvoiceNo = "WARRANTY-ONLY",
                AmpereInvoiceDate = today,
                ReceivedDate = today,
                IsAllocated = false,
                WarrantyOnly = true,
                Remarks = request.Remarks,
                CreatedBy = request.CreatedBy,
                CreatedDate = DateTime.UtcNow,
                ModifiedBy = request.CreatedBy,
                ModifiedDate = DateTime.UtcNow
            };

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var id = await _unitOfWork.VehicleMasters.AddAsync(master);
                await _unitOfWork.VehicleMasters.AddHistoryAsync(new VehicleMasterHistory
                {
                    VehicleMasterId = id,
                    Action = "WarrantyOnlyCreated",
                    Remarks = request.Remarks,
                    UserId = request.CreatedBy
                });

                master.VehicleMasterId = id;
                await WarrantyOnlyVehicleFlowHelper.ProvisionSoldVehicleAsync(
                    _unitOfWork,
                    master,
                    subdealerUserId,
                    request.CreatedBy,
                    request.CustomerName,
                    request.CustomerMobile,
                    request.SaleDate);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return id;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }

    public class UpdateWarrantyOnlyVehicleMasterCommandHandler : IRequestHandler<UpdateWarrantyOnlyVehicleMasterCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateWarrantyOnlyVehicleMasterCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<bool> Handle(UpdateWarrantyOnlyVehicleMasterCommand request, CancellationToken cancellationToken)
        {
            var master = await _unitOfWork.VehicleMasters.GetByIdAsync(request.VehicleMasterId)
                ?? throw new InvalidOperationException("Vehicle master record not found.");
            if (!master.WarrantyOnly)
                throw new InvalidOperationException("Only warranty-only vehicles can be edited here.");

            await ModelColorValidation.EnsureMappedAsync(_unitOfWork, request.ModelId, request.ColorId);

            master.ModelId = request.ModelId;
            master.ColorId = request.ColorId;
            master.Remarks = request.Remarks;
            master.ModifiedBy = request.ModifiedBy;
            master.ModifiedDate = DateTime.UtcNow;

            await _unitOfWork.VehicleMasters.UpdateAsync(master);

            var vehicle = (await _unitOfWork.Vehicles.GetAllAsync())
                .Where(v => v.VehicleMasterId == master.VehicleMasterId)
                .OrderByDescending(v => v.CreatedDate)
                .FirstOrDefault();
            if (vehicle != null)
            {
                vehicle.ModelId = request.ModelId;
                vehicle.ColorId = request.ColorId;
                vehicle.ModifiedDate = DateTime.UtcNow;
                await _unitOfWork.Vehicles.UpdateAsync(vehicle);

                var booking = (await _unitOfWork.VehicleBookings.GetAllAsync())
                    .FirstOrDefault(b => b.VehicleId == vehicle.VehicleId);
                if (booking != null)
                {
                    booking.CustomerName = WarrantyOnlyVehicleFlowHelper.NormalizeCustomerValue(request.CustomerName);
                    booking.CustomerMobile = WarrantyOnlyVehicleFlowHelper.NormalizeCustomerValue(request.CustomerMobile);
                    if (request.SaleDate.HasValue)
                        booking.SubmittedDate = request.SaleDate.Value.Date;
                    booking.ModifiedBy = request.ModifiedBy;
                    booking.ModifiedDate = DateTime.UtcNow;
                    await _unitOfWork.VehicleBookings.UpdateAsync(booking);
                }

                await WarrantyOnlyVehicleFlowHelper.SyncOperationalSubdealerAsync(
                    _unitOfWork, master, vehicle, booking);
            }

            await _unitOfWork.VehicleMasters.AddHistoryAsync(new VehicleMasterHistory
            {
                VehicleMasterId = master.VehicleMasterId,
                Action = "WarrantyOnlyEdited",
                Remarks = request.Remarks,
                UserId = request.ModifiedBy
            });
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }

    public class ImportWarrantyOnlyVehicleMastersCommandHandler : IRequestHandler<ImportWarrantyOnlyVehicleMastersCommand, ImportVehicleMastersResult>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ImportWarrantyOnlyVehicleMastersCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<ImportVehicleMastersResult> Handle(ImportWarrantyOnlyVehicleMastersCommand request, CancellationToken cancellationToken)
        {
            var result = new ImportVehicleMastersResult();
            if (request.Rows == null || request.Rows.Count == 0)
            {
                result.Errors.Add("No rows to import.");
                return result;
            }

            if (request.DefaultDealershipId <= 0
                && !request.Rows.Any(r => r.DealershipId > 0))
            {
                result.Errors.Add("Dealership is required (set DealershipCode on each row or use a scoped account).");
                return result;
            }

            var ownShowroomOrgs = (await _unitOfWork.SubDealers.GetAllAsync())
                .Where(o => o.OwnShowroom && o.IsActive)
                .ToDictionary(o => o.SubDealerId);

            var models = (await _unitOfWork.VehicleModels.GetAllAsync())
                .Where(m => m.IsActive)
                .ToDictionary(m => m.ModelId);
            var modelsByName = models.Values
                .GroupBy(m => m.ModelName.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var colors = (await _unitOfWork.VehicleColors.GetAllAsync())
                .Where(c => c.IsActive)
                .ToDictionary(c => c.ColorId);
            var colorsByName = colors.Values
                .GroupBy(c => c.ColorName.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var fileChassis = request.Rows
                .Select(r => r.ChassisNumber?.Trim().ToUpperInvariant() ?? "")
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();
            var dupInFile = fileChassis.GroupBy(c => c).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (dupInFile.Count > 0)
            {
                result.Errors.Add($"Duplicate chassis in file: {string.Join(", ", dupInFile)}");
                return result;
            }

            for (int i = 0; i < request.Rows.Count; i++)
            {
                var row = request.Rows[i];
                var line = i + 2;
                var chassis = row.ChassisNumber?.Trim().ToUpperInvariant() ?? "";
                if (string.IsNullOrWhiteSpace(chassis))
                {
                    result.Errors.Add($"Row {line}: ChassisNumber is required.");
                    continue;
                }

                if (row.SubDealerId <= 0)
                {
                    result.Errors.Add($"Row {line}: SubDealerId is required (Own Showroom org id).");
                    continue;
                }

                if (!ownShowroomOrgs.TryGetValue(row.SubDealerId, out var org))
                {
                    result.Errors.Add($"Row {line}: SubDealerId {row.SubDealerId} is not an active Own Showroom subdealer.");
                    continue;
                }

                var dealershipId = row.DealershipId > 0 ? row.DealershipId : request.DefaultDealershipId;
                if (org.DealershipId != dealershipId)
                {
                    result.Errors.Add($"Row {line}: SubDealerId {row.SubDealerId} does not belong to this dealership.");
                    continue;
                }

                try
                {
                    await WarrantyOnlyVehicleFlowHelper.ResolveOwnShowroomAsync(
                        _unitOfWork, dealershipId, row.SubDealerId);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Row {line}: {ex.Message}");
                    continue;
                }

                if (!ImportVehicleMastersCommandHandler.TryResolveModel(
                        new ImportVehicleMasterRow { ModelId = row.ModelId, ModelName = row.ModelName },
                        models, modelsByName, out var model, out var modelError))
                {
                    result.Errors.Add($"Row {line}: {modelError}");
                    continue;
                }

                if (!ImportVehicleMastersCommandHandler.TryResolveColor(
                        new ImportVehicleMasterRow { ColorId = row.ColorId, ColorName = row.ColorName },
                        colors, colorsByName, out var color, out var colorError))
                {
                    result.Errors.Add($"Row {line}: {colorError}");
                    continue;
                }

                try
                {
                    await GlobalUniqueValidation.EnsureChassisAvailableAsync(_unitOfWork, chassis);
                    await ModelColorValidation.EnsureMappedAsync(_unitOfWork, model.ModelId, color.ColorId);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Row {line}: {ex.Message}");
                }
            }

            if (result.Errors.Count > 0)
                return result;

            var today = DateTime.UtcNow.Date;
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var row in request.Rows)
                {
                    ImportVehicleMastersCommandHandler.TryResolveModel(
                        new ImportVehicleMasterRow { ModelId = row.ModelId, ModelName = row.ModelName },
                        models, modelsByName, out var model, out _);
                    ImportVehicleMastersCommandHandler.TryResolveColor(
                        new ImportVehicleMasterRow { ColorId = row.ColorId, ColorName = row.ColorName },
                        colors, colorsByName, out var color, out _);

                    var chassis = row.ChassisNumber.Trim().ToUpperInvariant();
                    var dealershipId = row.DealershipId > 0 ? row.DealershipId : request.DefaultDealershipId;
                    var (_, subdealerUserId) = await WarrantyOnlyVehicleFlowHelper.ResolveOwnShowroomAsync(
                        _unitOfWork, dealershipId, row.SubDealerId);

                    var master = new VehicleMaster
                    {
                        DealershipId = dealershipId,
                        ChassisNumber = chassis,
                        ModelId = model.ModelId,
                        ColorId = color.ColorId,
                        MotorNo = "-",
                        BatteryNo = "-",
                        ChargerNo = "-",
                        ControllerNo = "-",
                        ConverterNo = "-",
                        AmpereInvoiceNo = "WARRANTY-ONLY",
                        AmpereInvoiceDate = today,
                        ReceivedDate = today,
                        IsAllocated = false,
                        WarrantyOnly = true,
                        Remarks = row.Remarks,
                        CreatedBy = request.ImportedBy,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedBy = request.ImportedBy,
                        ModifiedDate = DateTime.UtcNow
                    };

                    var id = await _unitOfWork.VehicleMasters.AddAsync(master);
                    await _unitOfWork.VehicleMasters.AddHistoryAsync(new VehicleMasterHistory
                    {
                        VehicleMasterId = id,
                        Action = "WarrantyOnlyImported",
                        Remarks = row.Remarks,
                        UserId = request.ImportedBy
                    });

                    master.VehicleMasterId = id;
                    await WarrantyOnlyVehicleFlowHelper.ProvisionSoldVehicleAsync(
                        _unitOfWork,
                        master,
                        subdealerUserId,
                        request.ImportedBy,
                        row.CustomerName,
                        row.CustomerMobile,
                        row.SaleDate);

                    result.ImportedCount++;
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            return result;
        }
    }
}
