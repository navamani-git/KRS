using System.Globalization;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Web.Helpers.ExcelImport;
using KRSDealerManagement.Web.Models;
using KRSDealerManagement.Web.Services.ExcelImport;

namespace KRSDealerManagement.Web.Services.ExcelImport.Processors
{
    public sealed class RtoLocationsImportProcessor : IExcelImportProcessor
    {
        public string Key => ExcelImportKeys.RtoLocations;
        public string TemplateFileName => "import_rto_locations_sample.xlsx";
        public string DataSheetName => "RTO Locations";
        public IReadOnlyList<string> DataHeaders => new[] { "DistrictId", "LocationName" };
        public IReadOnlyList<IReadOnlyList<object?>> ExampleRows => new[] { new List<object?> { 1, "Mettur" } };

        public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetLookupsAsync(ExcelImportContext context)
        {
            await Task.CompletedTask;
            return new Dictionary<string, IReadOnlyList<string>>
            {
                ["DistrictId"] = new[] { "Use DistrictId from the Districts table on the Lookups sheet." }
            };
        }

        public async Task<IReadOnlyList<ExcelReferenceTable>> GetReferenceTablesAsync(ExcelImportContext context)
        {
            var districts = (await ExcelImportLookupHelper.GetRtoDistrictsAsync(context))
                .Where(d => d.IsActive)
                .OrderBy(d => d.DistrictName)
                .ToList();
            return new List<ExcelReferenceTable>
            {
                new()
                {
                    Title = "Districts",
                    Headers = new[] { "DistrictId", "DistrictName" },
                    Rows = districts.Select(d => (IReadOnlyList<object?>)new List<object?> { d.RtoDistrictId, d.DistrictName }).ToList()
                }
            };
        }

        public async Task<IReadOnlyList<ExcelImportError>> ValidateAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var errors = new List<ExcelImportError>();
            var uow = context.Services.GetRequiredService<IUnitOfWork>();
            var districts = (await uow.RtoDistricts.GetAllAsync()).Where(d => d.IsActive).ToDictionary(d => d.RtoDistrictId);
            var existing = (await uow.RtoLocations.GetAllAsync()).Select(r => r.LocationName.ToUpperInvariant()).ToHashSet();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                if (!int.TryParse(row.Get("DistrictId"), out var districtId) || districtId <= 0 || !districts.ContainsKey(districtId))
                {
                    errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "DistrictId", Message = "Valid DistrictId is required (see Lookups sheet)." });
                    continue;
                }

                var name = ExcelImportValidationHelper.Require(row, "LocationName", errors);
                if (name == null) continue;
                ExcelImportValidationHelper.DuplicateInFile(row, name, seen, "LocationName", errors);
                if (existing.Contains(name.ToUpperInvariant()))
                    errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "LocationName", Message = $"Location '{name}' already exists." });
            }
            return errors;
        }

        public async Task<int> InsertAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var uow = context.Services.GetRequiredService<IUnitOfWork>();
            foreach (var row in rows)
            {
                await uow.RtoLocations.AddAsync(new RtoLocationMaster
                {
                    RtoDistrictId = int.Parse(row.Get("DistrictId")!),
                    LocationName = row.Get("LocationName")!.Trim(),
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                });
            }
            await uow.SaveChangesAsync();
            return rows.Count;
        }
    }

    public sealed class DocumentTypesImportProcessor : IExcelImportProcessor
    {
        public string Key => ExcelImportKeys.DocumentTypes;
        public string TemplateFileName => "import_document_types_sample.xlsx";
        public string DataSheetName => "Document Types";
        public IReadOnlyList<string> DataHeaders => new[] { "TypeName" };
        public IReadOnlyList<IReadOnlyList<object?>> ExampleRows => new[] { new List<object?> { "Aadhaar" } };

        public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetLookupsAsync(ExcelImportContext context)
            => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>(
                new Dictionary<string, IReadOnlyList<string>>());

        public async Task<IReadOnlyList<ExcelImportError>> ValidateAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var errors = new List<ExcelImportError>();
            var uow = context.Services.GetRequiredService<IUnitOfWork>();
            var existing = (await uow.DocumentTypes.GetAllAsync()).Select(d => d.TypeName.ToUpperInvariant()).ToHashSet();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                var name = ExcelImportValidationHelper.Require(row, "TypeName", errors);
                if (name == null) continue;
                ExcelImportValidationHelper.DuplicateInFile(row, name, seen, "TypeName", errors);
                if (existing.Contains(name.ToUpperInvariant()))
                    errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "TypeName", Message = $"Type '{name}' already exists." });
            }
            return errors;
        }

        public async Task<int> InsertAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var uow = context.Services.GetRequiredService<IUnitOfWork>();
            foreach (var row in rows)
            {
                await uow.DocumentTypes.AddAsync(new DocumentTypeMaster
                {
                    TypeName = row.Get("TypeName")!.Trim(),
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                });
            }
            await uow.SaveChangesAsync();
            return rows.Count;
        }
    }

    public sealed class FinanceNamesImportProcessor : IExcelImportProcessor
    {
        public string Key => ExcelImportKeys.FinanceNames;
        public string TemplateFileName => "import_finance_names_sample.xlsx";
        public string DataSheetName => "Finance Names";
        public IReadOnlyList<string> DataHeaders => new[] { "FinanceName" };
        public IReadOnlyList<IReadOnlyList<object?>> ExampleRows => new[] { new List<object?> { "HDFC Bank" } };

        public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetLookupsAsync(ExcelImportContext context)
            => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>(new Dictionary<string, IReadOnlyList<string>>());

        public async Task<IReadOnlyList<ExcelImportError>> ValidateAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var errors = new List<ExcelImportError>();
            var uow = context.Services.GetRequiredService<IUnitOfWork>();
            var existing = (await uow.FinanceNames.GetAllAsync()).Select(f => f.FinanceName.ToUpperInvariant()).ToHashSet();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                var name = ExcelImportValidationHelper.Require(row, "FinanceName", errors);
                if (name == null) continue;
                ExcelImportValidationHelper.DuplicateInFile(row, name, seen, "FinanceName", errors);
                if (existing.Contains(name.ToUpperInvariant()))
                    errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "FinanceName", Message = $"Finance name '{name}' already exists." });
            }
            return errors;
        }

        public async Task<int> InsertAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var uow = context.Services.GetRequiredService<IUnitOfWork>();
            foreach (var row in rows)
            {
                await uow.FinanceNames.AddAsync(new FinanceNameMaster
                {
                    FinanceName = row.Get("FinanceName")!.Trim(),
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                });
            }
            await uow.SaveChangesAsync();
            return rows.Count;
        }
    }

    public sealed class DealershipsImportProcessor : IExcelImportProcessor
    {
        public string Key => ExcelImportKeys.Dealerships;
        public string TemplateFileName => "import_dealerships_sample.xlsx";
        public string DataSheetName => "Dealerships";
        public IReadOnlyList<string> DataHeaders => new[] { "DealershipCode", "DealershipName", "Location", "ContactPhone", "Email" };
        public IReadOnlyList<IReadOnlyList<object?>> ExampleRows => new[]
        {
            new List<object?> { "KRS_SALEM", "KRS Salem", "Salem", "9876543210", "salem@krs.com" }
        };

        public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetLookupsAsync(ExcelImportContext context)
            => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>(
                new Dictionary<string, IReadOnlyList<string>> { ["Instructions"] = new[] { "DealershipCode must be unique." } });

        public async Task<IReadOnlyList<ExcelImportError>> ValidateAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var errors = new List<ExcelImportError>();
            var uow = context.Services.GetRequiredService<IUnitOfWork>();
            var existing = (await uow.Dealerships.GetAllAsync()).Select(d => d.DealershipCode.ToUpperInvariant()).ToHashSet();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                var code = ExcelImportValidationHelper.Require(row, "DealershipCode", errors);
                ExcelImportValidationHelper.Require(row, "DealershipName", errors);
                if (code == null) continue;
                var normalized = code.Trim().ToUpperInvariant().Replace(" ", "_");
                ExcelImportValidationHelper.DuplicateInFile(row, normalized, seen, "DealershipCode", errors);
                if (existing.Contains(normalized))
                    errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "DealershipCode", Message = $"Code '{code}' already exists." });
            }
            return errors;
        }

        public async Task<int> InsertAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var uow = context.Services.GetRequiredService<IUnitOfWork>();
            foreach (var row in rows)
            {
                await uow.Dealerships.AddAsync(new Dealership
                {
                    DealershipCode = row.Get("DealershipCode")!.Trim().ToUpperInvariant().Replace(" ", "_"),
                    DealershipName = row.Get("DealershipName")!.Trim(),
                    Location = row.Get("Location")?.Trim(),
                    ContactPhone = row.Get("ContactPhone")?.Trim(),
                    Email = row.Get("Email")?.Trim(),
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                });
            }
            await uow.SaveChangesAsync();
            return rows.Count;
        }
    }

    public sealed class PaymentTypesImportProcessor : IExcelImportProcessor
    {
        public string Key => ExcelImportKeys.PaymentTypes;
        public string TemplateFileName => "import_payment_types_sample.xlsx";
        public string DataSheetName => "Payment Types";
        public IReadOnlyList<string> DataHeaders => new[] { "TypeCode", "TypeName", "SortOrder", "RequiresFinanceDetails" };
        public IReadOnlyList<IReadOnlyList<object?>> ExampleRows => new[]
        {
            new List<object?> { "FINANCE", "Finance", 2, "Yes" }
        };

        public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetLookupsAsync(ExcelImportContext context)
            => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>(
                new Dictionary<string, IReadOnlyList<string>>
                {
                    ["RequiresFinanceDetails"] = new[] { "Yes", "No" }
                });

        public async Task<IReadOnlyList<ExcelImportError>> ValidateAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var errors = new List<ExcelImportError>();
            var uow = context.Services.GetRequiredService<IUnitOfWork>();
            var all = (await uow.PaymentTypes.GetAllAsync()).ToList();
            var seenCode = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                ExcelImportValidationHelper.Require(row, "TypeCode", errors);
                ExcelImportValidationHelper.Require(row, "TypeName", errors);
                ExcelImportValidationHelper.TryInt(row, "SortOrder", errors, out _);
                ExcelImportValidationHelper.TryYesNo(row, "RequiresFinanceDetails", errors, out _);
                var code = row.Get("TypeCode")?.Trim();
                if (code != null)
                {
                    ExcelImportValidationHelper.DuplicateInFile(row, code, seenCode, "TypeCode", errors);
                    if (all.Any(t => t.TypeCode.Equals(code, StringComparison.OrdinalIgnoreCase)))
                        errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "TypeCode", Message = $"Code '{code}' already exists." });
                }
            }
            return errors;
        }

        public async Task<int> InsertAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var uow = context.Services.GetRequiredService<IUnitOfWork>();
            foreach (var row in rows)
            {
                ExcelImportValidationHelper.TryYesNo(row, "RequiresFinanceDetails", new List<ExcelImportError>(), out var reqFinance);
                await uow.PaymentTypes.AddAsync(new PaymentType
                {
                    TypeCode = row.Get("TypeCode")!.Trim().ToUpperInvariant(),
                    TypeName = row.Get("TypeName")!.Trim(),
                    SortOrder = int.Parse(row.Get("SortOrder")!),
                    RequiresFinanceDetails = reqFinance,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                });
            }
            await uow.SaveChangesAsync();
            return rows.Count;
        }
    }

    public sealed class VehicleMastersImportProcessor : IExcelImportProcessor
    {
        public string Key => ExcelImportKeys.VehicleMasters;
        public string TemplateFileName => "import_dealer_stock_sample.xlsx";
        public string DataSheetName => "Dealer Stock";
        public IReadOnlyList<string> DataHeaders => new[]
        {
            "DealershipCode", "ChassisNumber", "ModelId", "ColorId", "MotorNo", "BatteryNo", "ChargerNo",
            "ControllerNo", "ConverterNo", "ManufacturingYear", "AmpereInvoiceDate", "ReceivedDate", "Remarks"
        };
        public IReadOnlyList<IReadOnlyList<object?>> ExampleRows => new[]
        {
            new List<object?> { "SALEM", "CHASSIS001", 1, 1, "MOT001", "BAT001", "CHG001", "CTRL001", "CONV001", 2025, DateTime.Today, DateTime.Today, "" }
        };

        public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetLookupsAsync(ExcelImportContext context)
        {
            var dealerships = await ExcelImportLookupHelper.GetDealershipsAsync(context);
            return new Dictionary<string, IReadOnlyList<string>>
            {
                ["DealershipCode"] = dealerships.Select(d => d.DealershipCode).ToList(),
                ["ModelId"] = new[] { "Use ModelId from the Models table on the Lookups sheet." },
                ["ColorId"] = new[] { "Use ColorId from the Colors table on the Lookups sheet." }
            };
        }

        public async Task<IReadOnlyList<ExcelReferenceTable>> GetReferenceTablesAsync(ExcelImportContext context)
        {
            var dealerships = (await ExcelImportLookupHelper.GetDealershipsAsync(context))
                .OrderBy(d => d.DealershipName)
                .ToList();
            var models = (await ExcelImportLookupHelper.GetModelsAsync(context))
                .Where(m => m.IsActive)
                .OrderBy(m => m.ModelName)
                .ToList();
            var colors = (await ExcelImportLookupHelper.GetColorsAsync(context))
                .Where(c => c.IsActive)
                .OrderBy(c => c.ColorName)
                .ToList();

            return new List<ExcelReferenceTable>
            {
                new()
                {
                    Title = "Dealerships",
                    Headers = new[] { "DealershipCode", "DealershipName" },
                    Rows = dealerships.Select(d => (IReadOnlyList<object?>)new List<object?> { d.DealershipCode, d.DealershipName }).ToList()
                },
                new()
                {
                    Title = "Models",
                    Headers = new[] { "ModelId", "ModelName" },
                    Rows = models.Select(m => (IReadOnlyList<object?>)new List<object?> { m.ModelId, m.ModelName }).ToList()
                },
                new()
                {
                    Title = "Colors",
                    Headers = new[] { "ColorId", "ColorName" },
                    Rows = colors.Select(c => (IReadOnlyList<object?>)new List<object?> { c.ColorId, c.ColorName }).ToList()
                }
            };
        }

        public async Task<IReadOnlyList<ExcelImportError>> ValidateAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var errors = new List<ExcelImportError>();
            var dealerships = await ExcelImportLookupHelper.GetDealershipsAsync(context);
            var models = (await ExcelImportLookupHelper.GetModelsAsync(context))
                .Where(m => m.IsActive)
                .ToDictionary(m => m.ModelId);
            var colors = (await ExcelImportLookupHelper.GetColorsAsync(context))
                .Where(c => c.IsActive)
                .ToDictionary(c => c.ColorId);
            var uow = context.Services.GetRequiredService<IUnitOfWork>();

            var chassisInFile = rows
                .Select(r => r.Get("ChassisNumber")?.Trim().ToUpperInvariant() ?? "")
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();
            foreach (var dup in chassisInFile.GroupBy(c => c).Where(g => g.Count() > 1).Select(g => g.Key))
                errors.Add(new ExcelImportError { RowNumber = 0, Message = $"Duplicate chassis in file: {dup}" });

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var line = row.RowNumber > 0 ? row.RowNumber : i + 2;
                var dealerCode = ExcelImportValidationHelper.Require(row, "DealershipCode", errors);
                if (dealerCode != null)
                {
                    var dealer = ExcelImportLookupHelper.FindDealership(dealerships, dealerCode);
                    if (dealer == null)
                        errors.Add(new ExcelImportError { RowNumber = line, Column = "DealershipCode", Message = $"Unknown dealership '{dealerCode}'." });
                    else if (context.DealershipScopeId.HasValue && dealer.DealershipId != context.DealershipScopeId)
                        errors.Add(new ExcelImportError { RowNumber = line, Column = "DealershipCode", Message = "Dealership is outside your scope." });
                }

                var chassis = row.Get("ChassisNumber")?.Trim().ToUpperInvariant() ?? "";
                if (string.IsNullOrWhiteSpace(chassis))
                    errors.Add(new ExcelImportError { RowNumber = line, Column = "ChassisNumber", Message = "ChassisNumber is required." });
                if (!int.TryParse(row.Get("ModelId"), out var modelId) || modelId <= 0)
                    errors.Add(new ExcelImportError { RowNumber = line, Column = "ModelId", Message = "ModelId must be a whole number from the Lookups sheet." });
                if (!int.TryParse(row.Get("ColorId"), out var colorId) || colorId <= 0)
                    errors.Add(new ExcelImportError { RowNumber = line, Column = "ColorId", Message = "ColorId must be a whole number from the Lookups sheet." });
                if (string.IsNullOrWhiteSpace(row.Get("MotorNo")))
                    errors.Add(new ExcelImportError { RowNumber = line, Column = "MotorNo", Message = "MotorNo is required." });
                if (string.IsNullOrWhiteSpace(row.Get("BatteryNo")))
                    errors.Add(new ExcelImportError { RowNumber = line, Column = "BatteryNo", Message = "BatteryNo is required." });
                if (string.IsNullOrWhiteSpace(row.Get("ChargerNo")))
                    errors.Add(new ExcelImportError { RowNumber = line, Column = "ChargerNo", Message = "ChargerNo is required." });
                if (string.IsNullOrWhiteSpace(row.Get("ControllerNo")))
                    errors.Add(new ExcelImportError { RowNumber = line, Column = "ControllerNo", Message = "ControllerNo is required." });
                if (string.IsNullOrWhiteSpace(row.Get("ConverterNo")))
                    errors.Add(new ExcelImportError { RowNumber = line, Column = "ConverterNo", Message = "ConverterNo is required." });
                if (!int.TryParse(row.Get("ManufacturingYear"), out var year) || year <= 0)
                    errors.Add(new ExcelImportError { RowNumber = line, Column = "ManufacturingYear", Message = "ManufacturingYear must be a whole number." });
                if (!DateTime.TryParse(row.Get("AmpereInvoiceDate"), CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                    && !DateTime.TryParse(row.Get("AmpereInvoiceDate"), out _))
                    errors.Add(new ExcelImportError { RowNumber = line, Column = "AmpereInvoiceDate", Message = "AmpereInvoiceDate must be a valid date (yyyy-MM-dd)." });
                if (!DateTime.TryParse(row.Get("ReceivedDate"), CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                    && !DateTime.TryParse(row.Get("ReceivedDate"), out _))
                    errors.Add(new ExcelImportError { RowNumber = line, Column = "ReceivedDate", Message = "ReceivedDate must be a valid date (yyyy-MM-dd)." });

                if (string.IsNullOrWhiteSpace(chassis)
                    || modelId <= 0 || colorId <= 0
                    || string.IsNullOrWhiteSpace(row.Get("MotorNo"))
                    || string.IsNullOrWhiteSpace(row.Get("BatteryNo"))
                    || string.IsNullOrWhiteSpace(row.Get("ChargerNo"))
                    || string.IsNullOrWhiteSpace(row.Get("ControllerNo"))
                    || string.IsNullOrWhiteSpace(row.Get("ConverterNo"))
                    || year <= 0)
                    continue;

                if (!models.ContainsKey(modelId))
                    errors.Add(new ExcelImportError { RowNumber = line, Column = "ModelId", Message = $"Unknown model id '{modelId}'." });
                if (!colors.ContainsKey(colorId))
                    errors.Add(new ExcelImportError { RowNumber = line, Column = "ColorId", Message = $"Unknown color id '{colorId}'." });
                if (await uow.VehicleMasters.ChassisExistsAsync(chassis))
                    errors.Add(new ExcelImportError { RowNumber = line, Column = "ChassisNumber", Message = $"Chassis '{chassis}' already exists." });
            }

            return errors;
        }

        public async Task<int> InsertAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var dealerships = await ExcelImportLookupHelper.GetDealershipsAsync(context);
            var importRows = rows.Select(r => MapRow(r, dealerships, context)).ToList();
            var result = await context.Services.GetRequiredService<MediatR.IMediator>().Send(
                new KRSDealerManagement.Application.Commands.ImportVehicleMastersCommand
                {
                    DealershipId = context.DealershipScopeId ?? 0,
                    ImportedBy = context.UserId,
                    Rows = importRows
                });

            if (!result.Success)
                throw new InvalidOperationException(string.Join(" ", result.Errors));

            return result.ImportedCount;
        }

        private static KRSDealerManagement.Application.Commands.ImportVehicleMasterRow MapRow(
            ExcelImportRow row,
            IReadOnlyList<DealershipDto> dealerships,
            ExcelImportContext context)
        {
            var dealer = ExcelImportLookupHelper.FindDealership(dealerships, row.Get("DealershipCode"));
            var dealershipId = context.DealershipScopeId ?? dealer?.DealershipId ?? 0;
            return new()
            {
                DealershipId = dealershipId,
                ChassisNumber = row.Get("ChassisNumber")?.Trim() ?? "",
                ModelId = int.TryParse(row.Get("ModelId"), out var modelId) ? modelId : null,
                ColorId = int.TryParse(row.Get("ColorId"), out var colorId) ? colorId : null,
                MotorNo = row.Get("MotorNo")?.Trim() ?? "",
                BatteryNo = row.Get("BatteryNo")?.Trim() ?? "",
                ChargerNo = row.Get("ChargerNo")?.Trim() ?? "",
                ControllerNo = row.Get("ControllerNo")?.Trim() ?? "",
                ConverterNo = row.Get("ConverterNo")?.Trim() ?? "",
                ManufacturingYear = int.TryParse(row.Get("ManufacturingYear"), out var y) ? y : 0,
                AmpereInvoiceDate = ParseImportDate(row.Get("AmpereInvoiceDate")),
                ReceivedDate = ParseImportDate(row.Get("ReceivedDate")),
                Remarks = row.Get("Remarks")?.Trim()
            };
        }

        private static DateTime ParseImportDate(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return default;

            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                || DateTime.TryParse(raw, out dt))
                return dt.Date;

            return default;
        }
    }
}
