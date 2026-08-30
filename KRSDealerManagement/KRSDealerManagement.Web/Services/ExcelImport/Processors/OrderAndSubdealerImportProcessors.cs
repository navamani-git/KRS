using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Helpers.ExcelImport;
using KRSDealerManagement.Web.Models;
using KRSDealerManagement.Web.Services.ExcelImport;

namespace KRSDealerManagement.Web.Services.ExcelImport.Processors
{
    public sealed class SubdealersImportProcessor : IExcelImportProcessor
    {
        public string Key => ExcelImportKeys.Subdealers;
        public string TemplateFileName => "import_subdealers_sample.xlsx";
        public string DataSheetName => "Subdealers";
        public IReadOnlyList<string> DataHeaders => new[]
        {
            "SubdealerName", "Email", "Location", "PrimaryPhone", "SecondaryPhone", "SalesRepMobile", "ServiceRepMobile", "DealershipCode"
        };
        public IReadOnlyList<IReadOnlyList<object?>> ExampleRows => new[]
        {
            new List<object?> { "ABC Motors", "abc@example.com", "Salem", "9876543210", "", "9876543211", "9876543212", "KRS_SALEM" }
        };

        public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetLookupsAsync(ExcelImportContext context)
        {
            var dealerships = await ExcelImportLookupHelper.GetDealershipsAsync(context);
            return new Dictionary<string, IReadOnlyList<string>>
            {
                ["DealershipCode"] = dealerships.Select(d => d.DealershipCode).ToList()
            };
        }

        public async Task<IReadOnlyList<ExcelImportError>> ValidateAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            if (context.IsBranchManager)
                return new[] { new ExcelImportError { RowNumber = 0, Message = "Branch managers cannot import subdealers." } };

            var errors = new List<ExcelImportError>();
            var dealerships = await ExcelImportLookupHelper.GetDealershipsAsync(context);
            foreach (var row in rows)
            {
                ExcelImportValidationHelper.Require(row, "SubdealerName", errors);
                ExcelImportValidationHelper.Require(row, "Location", errors);
                ExcelImportValidationHelper.Require(row, "PrimaryPhone", errors);
                var code = ExcelImportValidationHelper.Require(row, "DealershipCode", errors);
                if (code != null && ExcelImportLookupHelper.FindDealership(dealerships, code) == null)
                    errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "DealershipCode", Message = $"Unknown dealership '{code}'." });
                if (context.DealershipScopeId.HasValue && code != null)
                {
                    var dealer = ExcelImportLookupHelper.FindDealership(dealerships, code);
                    if (dealer != null && dealer.DealershipId != context.DealershipScopeId)
                        errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "DealershipCode", Message = "Dealership is outside your scope." });
                }
            }
            return errors;
        }

        public async Task<int> InsertAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var mediator = context.Services.GetRequiredService<IMediator>();
            var dealerships = await ExcelImportLookupHelper.GetDealershipsAsync(context);
            foreach (var row in rows)
            {
                var dealer = ExcelImportLookupHelper.FindDealership(dealerships, row.Get("DealershipCode"))!;
                var dealershipId = context.DealershipScopeId ?? dealer.DealershipId;
                var name = row.Get("SubdealerName")!.Trim();
                await mediator.Send(new CreateSubdealerCommand
                {
                    SubdealerName = name,
                    Email = string.IsNullOrWhiteSpace(row.Get("Email"))
                        ? $"{name.ToLower().Replace(" ", ".")}@krs.com"
                        : row.Get("Email")!.Trim(),
                    Location = row.Get("Location")!.Trim(),
                    PrimaryPhone = row.Get("PrimaryPhone")!.Trim(),
                    SecondaryPhone = row.Get("SecondaryPhone")?.Trim(),
                    SalesRepMobile = row.Get("SalesRepMobile")?.Trim() ?? "",
                    ServiceRepMobile = row.Get("ServiceRepMobile")?.Trim() ?? "",
                    DealershipId = dealershipId,
                    CreatedBy = context.UserId
                });
            }
            return rows.Count;
        }
    }

    public sealed class SubdealerAccountsImportProcessor : IExcelImportProcessor
    {
        public string Key => ExcelImportKeys.SubdealerAccounts;
        public string TemplateFileName => "import_subdealer_accounts_sample.xlsx";
        public string DataSheetName => "Subdealer Accounts";
        public IReadOnlyList<string> DataHeaders => new[] { "SubdealerName", "AccountName", "AccountType", "Description", "InitialBalance" };
        public IReadOnlyList<IReadOnlyList<object?>> ExampleRows => new[]
        {
            new List<object?> { "ABC Motors", "Main Account", "Main", "Primary wallet", 0m }
        };

        public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetLookupsAsync(ExcelImportContext context)
        {
            var subdealers = await ExcelImportLookupHelper.GetSubdealersAsync(context);
            return new Dictionary<string, IReadOnlyList<string>>
            {
                ["SubdealerName"] = subdealers.Select(s => s.GetFullName()).ToList(),
                ["AccountType"] = new[] { "Main", "Secondary" }
            };
        }

        public async Task<IReadOnlyList<ExcelImportError>> ValidateAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var errors = new List<ExcelImportError>();
            var subdealers = await ExcelImportLookupHelper.GetSubdealersAsync(context);
            foreach (var row in rows)
            {
                var subName = ExcelImportValidationHelper.Require(row, "SubdealerName", errors);
                ExcelImportValidationHelper.Require(row, "AccountName", errors);
                ExcelImportValidationHelper.Require(row, "AccountType", errors);
                ExcelImportValidationHelper.TryDecimal(row, "InitialBalance", errors, out _);
                if (subName != null && ExcelImportLookupHelper.FindSubdealer(subdealers, subName) == null)
                    errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "SubdealerName", Message = $"Unknown subdealer '{subName}'." });
            }
            return errors;
        }

        public async Task<int> InsertAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var mediator = context.Services.GetRequiredService<IMediator>();
            var subdealers = await ExcelImportLookupHelper.GetSubdealersAsync(context);
            foreach (var row in rows)
            {
                var sub = ExcelImportLookupHelper.FindSubdealer(subdealers, row.Get("SubdealerName"))!;
                await mediator.Send(new CreateSubdealerAccountCommand
                {
                    SubdealerId = sub.UserId,
                    AccountName = row.Get("AccountName")!.Trim(),
                    AccountType = row.Get("AccountType")!.Trim(),
                    Description = row.Get("Description")?.Trim(),
                    InitialBalance = decimal.Parse(row.Get("InitialBalance") ?? "0"),
                    CreatedBy = context.UserId
                });
            }
            return rows.Count;
        }
    }

    public sealed class OrdersSubdealerImportProcessor : IExcelImportProcessor
    {
        public string Key => ExcelImportKeys.OrdersSubdealer;
        public string TemplateFileName => "import_purchase_order_sample.xlsx";
        public string DataSheetName => "Purchase Order";
        public IReadOnlyList<string> DataHeaders => new[] { "SubdealerNotes", "ModelName", "ColorName", "Quantity", "UnitPrice" };
        public IReadOnlyList<IReadOnlyList<object?>> ExampleRows => new[]
        {
            new List<object?> { "Urgent delivery", "Nexus E5", "Pearl White", 2, 85000m }
        };

        public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetLookupsAsync(ExcelImportContext context)
        {
            var models = await ExcelImportLookupHelper.GetModelsAsync(context);
            var colors = await ExcelImportLookupHelper.GetColorsAsync(context);
            return new Dictionary<string, IReadOnlyList<string>>
            {
                ["ModelName"] = models.Select(m => m.ModelName).ToList(),
                ["ColorName"] = colors.Select(c => c.ColorName).ToList()
            };
        }

        public async Task<IReadOnlyList<ExcelImportError>> ValidateAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var errors = new List<ExcelImportError>();
            var models = await ExcelImportLookupHelper.GetModelsAsync(context);
            var colors = await ExcelImportLookupHelper.GetColorsAsync(context);
            var mediator = context.Services.GetRequiredService<IMediator>();
            var account = await AccountHelper.GetPrimaryAccountAsync(mediator, context.UserId);
            if (account == null)
                errors.Add(new ExcelImportError { RowNumber = 0, Message = "No account found for your profile." });

            foreach (var row in rows)
            {
                var modelName = ExcelImportValidationHelper.Require(row, "ModelName", errors);
                var colorName = ExcelImportValidationHelper.Require(row, "ColorName", errors);
                ExcelImportValidationHelper.TryInt(row, "Quantity", errors, out var qty);
                ExcelImportValidationHelper.TryDecimal(row, "UnitPrice", errors, out var price);
                if (qty <= 0) errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "Quantity", Message = "Quantity must be greater than zero." });
                if (price <= 0) errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "UnitPrice", Message = "UnitPrice must be greater than zero." });
                if (ExcelImportLookupHelper.FindModel(models, modelName) == null)
                    errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "ModelName", Message = $"Unknown model '{modelName}'." });
                if (ExcelImportLookupHelper.FindColor(colors, colorName) == null)
                    errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "ColorName", Message = $"Unknown color '{colorName}'." });
            }
            return errors;
        }

        public async Task<int> InsertAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var mediator = context.Services.GetRequiredService<IMediator>();
            var models = await ExcelImportLookupHelper.GetModelsAsync(context);
            var colors = await ExcelImportLookupHelper.GetColorsAsync(context);
            var account = await AccountHelper.GetPrimaryAccountAsync(mediator, context.UserId)
                ?? throw new InvalidOperationException("No account found.");

            var items = rows.Select(row => new OrderItem
            {
                ModelId = ExcelImportLookupHelper.FindModel(models, row.Get("ModelName"))!.ModelId,
                ColorId = ExcelImportLookupHelper.FindColor(colors, row.Get("ColorName"))!.ColorId,
                Quantity = int.Parse(row.Get("Quantity")!),
                UnitPrice = decimal.Parse(row.Get("UnitPrice")!)
            }).ToList();

            var notes = rows.Select(r => r.Get("SubdealerNotes")?.Trim()).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));

            await mediator.Send(new CreatePurchaseOrderCommand
            {
                AccountId = account.AccountId,
                SubdealerId = context.UserId,
                Items = items,
                SubdealerNotes = notes,
                CreatedBy = context.UserId
            });
            return rows.Count;
        }
    }

    public sealed class OrdersForSubdealerImportProcessor : IExcelImportProcessor
    {
        public string Key => ExcelImportKeys.OrdersForSubdealer;
        public string TemplateFileName => "import_staff_order_sample.xlsx";
        public string DataSheetName => "Staff Order";
        public IReadOnlyList<string> DataHeaders => new[]
        {
            "SubdealerName", "AdminNotes", "ModelName", "ColorName", "UnitPrice", "ChassisNumber"
        };
        public IReadOnlyList<IReadOnlyList<object?>> ExampleRows => new[]
        {
            new List<object?> { "ABC Motors", "Showroom stock", "Nexus E5", "Pearl White", 85000m, "CHASSIS001" }
        };

        public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetLookupsAsync(ExcelImportContext context)
        {
            var models = await ExcelImportLookupHelper.GetModelsAsync(context);
            var colors = await ExcelImportLookupHelper.GetColorsAsync(context);
            var subdealers = await ExcelImportLookupHelper.GetSubdealersAsync(context);
            return new Dictionary<string, IReadOnlyList<string>>
            {
                ["ModelName"] = models.Select(m => m.ModelName).ToList(),
                ["ColorName"] = colors.Select(c => c.ColorName).ToList(),
                ["SubdealerName"] = subdealers.Select(s => s.GetFullName()).ToList()
            };
        }

        public async Task<IReadOnlyList<ExcelImportError>> ValidateAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var errors = new List<ExcelImportError>();
            var models = await ExcelImportLookupHelper.GetModelsAsync(context);
            var colors = await ExcelImportLookupHelper.GetColorsAsync(context);
            var subdealers = await ExcelImportLookupHelper.GetSubdealersAsync(context);

            var subNames = rows.Select(r => r.Get("SubdealerName")?.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (subNames.Count != 1)
            {
                errors.Add(new ExcelImportError { RowNumber = 0, Message = "All rows must belong to the same subdealer (same SubdealerName)." });
                return errors;
            }

            var sub = ExcelImportLookupHelper.FindSubdealer(subdealers, subNames[0]);
            if (sub == null)
            {
                errors.Add(new ExcelImportError { RowNumber = 0, Message = $"Unknown subdealer '{subNames[0]}'." });
                return errors;
            }

            var mediator = context.Services.GetRequiredService<IMediator>();
            var unitOfWork = context.Services.GetRequiredService<KRSDealerManagement.Domain.Repositories.IUnitOfWork>();
            var account = await AccountHelper.GetPrimaryAccountAsync(mediator, sub.UserId);
            if (account == null)
                errors.Add(new ExcelImportError { RowNumber = 0, Message = "No account found for the selected subdealer." });

            var chassisInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                ExcelImportValidationHelper.Require(row, "ModelName", errors);
                ExcelImportValidationHelper.Require(row, "ColorName", errors);
                ExcelImportValidationHelper.Require(row, "ChassisNumber", errors);
                ExcelImportValidationHelper.TryDecimal(row, "UnitPrice", errors, out var price);
                if (price <= 0) errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "UnitPrice", Message = "UnitPrice must be greater than zero." });
                var modelName = row.Get("ModelName");
                var colorName = row.Get("ColorName");
                var model = ExcelImportLookupHelper.FindModel(models, modelName);
                var color = ExcelImportLookupHelper.FindColor(colors, colorName);
                if (model == null)
                    errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "ModelName", Message = $"Unknown model '{modelName}'." });
                if (color == null)
                    errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "ColorName", Message = $"Unknown color '{colorName}'." });

                var chassis = row.Get("ChassisNumber")?.Trim().ToUpperInvariant() ?? "";
                if (!string.IsNullOrWhiteSpace(chassis))
                {
                    if (!chassisInFile.Add(chassis))
                        errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "ChassisNumber", Message = $"Duplicate chassis '{chassis}' in file." });

                    var master = await unitOfWork.VehicleMasters.GetByChassisAsync(chassis);
                    if (master == null)
                        errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "ChassisNumber", Message = $"Chassis '{chassis}' not found in dealer stock." });
                    else if (master.IsAllocated)
                        errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "ChassisNumber", Message = $"Chassis '{chassis}' is already allocated." });
                    else if (model != null && color != null && (master.ModelId != model.ModelId || master.ColorId != color.ColorId))
                        errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "ChassisNumber", Message = $"Chassis '{chassis}' does not match model/color." });
                    else if (context.DealershipScopeId.HasValue && master?.DealershipId != context.DealershipScopeId.Value)
                        errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "ChassisNumber", Message = $"Chassis '{chassis}' is outside your dealership scope." });
                }
            }
            return errors;
        }

        public async Task<int> InsertAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var mediator = context.Services.GetRequiredService<IMediator>();
            var unitOfWork = context.Services.GetRequiredService<KRSDealerManagement.Domain.Repositories.IUnitOfWork>();
            var models = await ExcelImportLookupHelper.GetModelsAsync(context);
            var colors = await ExcelImportLookupHelper.GetColorsAsync(context);
            var subdealers = await ExcelImportLookupHelper.GetSubdealersAsync(context);
            var subName = rows[0].Get("SubdealerName")!.Trim();
            var sub = ExcelImportLookupHelper.FindSubdealer(subdealers, subName)!;
            var account = await AccountHelper.GetPrimaryAccountAsync(mediator, sub.UserId)
                ?? throw new InvalidOperationException("No account found.");

            var items = new List<OrderItem>();
            foreach (var row in rows)
            {
                var chassis = row.Get("ChassisNumber")!.Trim().ToUpperInvariant();
                var master = await unitOfWork.VehicleMasters.GetByChassisAsync(chassis)
                    ?? throw new InvalidOperationException($"Chassis '{chassis}' not found.");
                items.Add(new OrderItem
                {
                    ModelId = ExcelImportLookupHelper.FindModel(models, row.Get("ModelName"))!.ModelId,
                    ColorId = ExcelImportLookupHelper.FindColor(colors, row.Get("ColorName"))!.ColorId,
                    Quantity = 1,
                    UnitPrice = decimal.Parse(row.Get("UnitPrice")!),
                    VehicleMasterId = master.VehicleMasterId
                });
            }

            var adminNotes = rows.Select(r => r.Get("AdminNotes")?.Trim()).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));

            await mediator.Send(new CreatePurchaseOrderCommand
            {
                AccountId = account.AccountId,
                SubdealerId = sub.UserId,
                Items = items,
                AdminNotes = adminNotes,
                AutoApprove = true,
                CreatedBy = context.UserId
            });
            return rows.Count;
        }
    }
}
