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
        public IReadOnlyList<string> DataHeaders => new[] { "LocationName" };
        public IReadOnlyList<IReadOnlyList<object?>> ExampleRows => new[] { new List<object?> { "Salem RTO" } };

        public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetLookupsAsync(ExcelImportContext context)
            => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>(
                new Dictionary<string, IReadOnlyList<string>> { ["Instructions"] = new[] { "LocationName must be unique." } });

        public async Task<IReadOnlyList<ExcelImportError>> ValidateAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var errors = new List<ExcelImportError>();
            var uow = context.Services.GetRequiredService<IUnitOfWork>();
            var existing = (await uow.RtoLocations.GetAllAsync()).Select(r => r.LocationName.ToUpperInvariant()).ToHashSet();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
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
}
