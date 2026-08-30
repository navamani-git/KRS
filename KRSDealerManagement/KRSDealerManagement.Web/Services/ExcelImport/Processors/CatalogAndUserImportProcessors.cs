using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Enums;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Helpers.ExcelImport;
using KRSDealerManagement.Web.Models;
using KRSDealerManagement.Web.Services.ExcelImport;

namespace KRSDealerManagement.Web.Services.ExcelImport.Processors
{
    internal static class ExcelImportLookupHelper
    {
        public static async Task<IReadOnlyList<VehicleModelDto>> GetModelsAsync(ExcelImportContext ctx)
            => (await ctx.Services.GetRequiredService<IMediator>().Send(new GetVehicleModelsQuery { IsActive = true })).ToList();

        public static async Task<IReadOnlyList<VehicleColorDto>> GetColorsAsync(ExcelImportContext ctx)
            => (await ctx.Services.GetRequiredService<IMediator>().Send(new GetVehicleColorsQuery { IsActive = true })).ToList();

        public static async Task<IReadOnlyList<DealershipDto>> GetDealershipsAsync(ExcelImportContext ctx)
            => (await ctx.Services.GetRequiredService<IMediator>().Send(new GetDealershipsQuery())).ToList();

        public static async Task<IReadOnlyList<UserDto>> GetSubdealersAsync(ExcelImportContext ctx)
            => (await ctx.Services.GetRequiredService<IMediator>().Send(new GetSubdealersQuery
            {
                IsActive = true,
                DealershipId = ctx.DealershipScopeId
            })).ToList();

        public static async Task<IReadOnlyList<RtoDistrictMaster>> GetRtoDistrictsAsync(ExcelImportContext ctx)
            => (await ctx.Services.GetRequiredService<IUnitOfWork>().RtoDistricts.GetAllAsync()).ToList();

        public static VehicleModelDto? FindModel(IEnumerable<VehicleModelDto> models, string? name)
            => models.FirstOrDefault(m => m.ModelName.Equals(name?.Trim(), StringComparison.OrdinalIgnoreCase));

        public static VehicleColorDto? FindColor(IEnumerable<VehicleColorDto> colors, string? name)
            => colors.FirstOrDefault(c => c.ColorName.Equals(name?.Trim(), StringComparison.OrdinalIgnoreCase));

        public static DealershipDto? FindDealership(IEnumerable<DealershipDto> list, string? codeOrName)
        {
            if (string.IsNullOrWhiteSpace(codeOrName)) return null;
            return list.FirstOrDefault(d =>
                d.DealershipCode.Equals(codeOrName.Trim(), StringComparison.OrdinalIgnoreCase)
                || d.DealershipName.Equals(codeOrName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public static UserDto? FindSubdealer(IEnumerable<UserDto> list, string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return list.FirstOrDefault(s =>
                s.GetFullName().Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)
                || (s.Email?.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase) ?? false));
        }
    }

    public sealed class VehicleColorsImportProcessor : IExcelImportProcessor
    {
        public string Key => ExcelImportKeys.VehicleColors;
        public string TemplateFileName => "import_vehicle_colors_sample.xlsx";
        public string DataSheetName => "Vehicle Colors";
        public IReadOnlyList<string> DataHeaders => new[] { "ColorName", "HexCode" };
        public IReadOnlyList<IReadOnlyList<object?>> ExampleRows => new[] { new List<object?> { "Pearl White", "#FFFFFF" } };

        public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetLookupsAsync(ExcelImportContext context)
            => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>(
                new Dictionary<string, IReadOnlyList<string>> { ["HexCode"] = new[] { "Optional. Example: #FFFFFF" } });

        public async Task<IReadOnlyList<ExcelImportError>> ValidateAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var errors = new List<ExcelImportError>();
            var colors = await ExcelImportLookupHelper.GetColorsAsync(context);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                var name = ExcelImportValidationHelper.Require(row, "ColorName", errors);
                if (name == null) continue;
                ExcelImportValidationHelper.DuplicateInFile(row, name, seen, "ColorName", errors);
                if (colors.Any(c => c.ColorName.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "ColorName", Message = $"Color '{name}' already exists." });
            }
            return errors;
        }

        public async Task<int> InsertAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var mediator = context.Services.GetRequiredService<IMediator>();
            foreach (var row in rows)
            {
                await mediator.Send(new CreateVehicleColorCommand
                {
                    ColorName = row.Get("ColorName")!.Trim(),
                    HexCode = string.IsNullOrWhiteSpace(row.Get("HexCode")) ? null : row.Get("HexCode")!.Trim(),
                    CreatedBy = context.UserId
                });
            }
            return rows.Count;
        }
    }

    public sealed class StatusLookupsImportProcessor : IExcelImportProcessor
    {
        public string Key => ExcelImportKeys.StatusLookups;
        public string TemplateFileName => "import_status_lookups_sample.xlsx";
        public string DataSheetName => "Status Lookups";
        public IReadOnlyList<string> DataHeaders => new[] { "Category", "StatusValue", "StatusCode", "StatusName", "BadgeClass", "SortOrder" };
        public IReadOnlyList<IReadOnlyList<object?>> ExampleRows => new[]
        {
            new List<object?> { "VEHICLE", 10, "BOOKED", "Booked to Customer", "bg-primary", 10 }
        };

        public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetLookupsAsync(ExcelImportContext context)
            => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>(
                new Dictionary<string, IReadOnlyList<string>>
                {
                    ["Category"] = StatusCategories.All.Select(c => $"{c.Code} ({c.Name})").ToList(),
                    ["BadgeClass"] = StatusBadgeOptions.All.Select(b => b.Value).ToList()
                });

        public async Task<IReadOnlyList<ExcelImportError>> ValidateAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var errors = new List<ExcelImportError>();
            var uow = context.Services.GetRequiredService<IUnitOfWork>();
            var all = (await uow.StatusLookups.GetAllAsync()).ToList();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                var catRaw = ExcelImportValidationHelper.Require(row, "Category", errors);
                ExcelImportValidationHelper.TryInt(row, "StatusValue", errors, out var val);
                var code = ExcelImportValidationHelper.Require(row, "StatusCode", errors);
                ExcelImportValidationHelper.Require(row, "StatusName", errors);
                ExcelImportValidationHelper.TryInt(row, "SortOrder", errors, out _);
                if (catRaw == null) continue;
                var cat = catRaw.Split('(')[0].Trim().ToUpperInvariant();
                if (!StatusCategories.IsValid(cat))
                    errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "Category", Message = $"Invalid category '{catRaw}'. Use VEHICLE, PAYMENT, or COMMISSION." });
                var key = $"{cat}|{val}";
                ExcelImportValidationHelper.DuplicateInFile(row, key, seen, "Category+StatusValue", errors);
                if (all.Any(s => s.Category.Equals(cat, StringComparison.OrdinalIgnoreCase) && s.StatusValue == val))
                    errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "StatusValue", Message = $"Status value {val} already exists for {cat}." });
                if (code != null && all.Any(s => s.Category.Equals(cat, StringComparison.OrdinalIgnoreCase) && s.StatusCode.Equals(code, StringComparison.OrdinalIgnoreCase)))
                    errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "StatusCode", Message = $"Status code '{code}' already exists for {cat}." });
            }
            return errors;
        }

        public async Task<int> InsertAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var uow = context.Services.GetRequiredService<IUnitOfWork>();
            foreach (var row in rows)
            {
                var cat = row.Get("Category")!.Split('(')[0].Trim().ToUpperInvariant();
                await uow.StatusLookups.AddAsync(new Domain.Entities.StatusLookup
                {
                    Category = cat,
                    StatusValue = int.Parse(row.Get("StatusValue")!),
                    StatusCode = row.Get("StatusCode")!.Trim().ToUpperInvariant(),
                    StatusName = row.Get("StatusName")!.Trim(),
                    BadgeClass = string.IsNullOrWhiteSpace(row.Get("BadgeClass")) ? "bg-secondary" : row.Get("BadgeClass")!.Trim(),
                    SortOrder = int.Parse(row.Get("SortOrder")!),
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                });
            }
            await uow.SaveChangesAsync();
            return rows.Count;
        }
    }

    public sealed class VehicleModelsImportProcessor : IExcelImportProcessor
    {
        public string Key => ExcelImportKeys.VehicleModels;
        public string TemplateFileName => "import_vehicle_models_sample.xlsx";
        public string DataSheetName => "Vehicle Models";
        public IReadOnlyList<string> DataHeaders => new[] { "ModelName", "Description", "Colors" };
        public IReadOnlyList<IReadOnlyList<object?>> ExampleRows => new[]
        {
            new List<object?> { "Nexus E5", "Electric scooter", "Pearl White, Midnight Black" }
        };

        public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetLookupsAsync(ExcelImportContext context)
        {
            var colors = await ExcelImportLookupHelper.GetColorsAsync(context);
            return new Dictionary<string, IReadOnlyList<string>>
            {
                ["Colors"] = colors.Select(c => c.ColorName).ToList()
            };
        }

        public async Task<IReadOnlyList<ExcelImportError>> ValidateAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var errors = new List<ExcelImportError>();
            var models = await ExcelImportLookupHelper.GetModelsAsync(context);
            var colors = await ExcelImportLookupHelper.GetColorsAsync(context);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                var name = ExcelImportValidationHelper.Require(row, "ModelName", errors);
                var colorsRaw = ExcelImportValidationHelper.Require(row, "Colors", errors);
                if (name == null) continue;
                ExcelImportValidationHelper.DuplicateInFile(row, name, seen, "ModelName", errors);
                if (models.Any(m => m.ModelName.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "ModelName", Message = $"Model '{name}' already exists." });
                if (colorsRaw != null)
                {
                    foreach (var cn in colorsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        if (ExcelImportLookupHelper.FindColor(colors, cn) == null)
                            errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "Colors", Message = $"Unknown color '{cn}'." });
                    }
                }
            }
            return errors;
        }

        public async Task<int> InsertAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var mediator = context.Services.GetRequiredService<IMediator>();
            var colors = await ExcelImportLookupHelper.GetColorsAsync(context);
            foreach (var row in rows)
            {
                var colorIds = row.Get("Colors")!
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(cn => ExcelImportLookupHelper.FindColor(colors, cn)!.ColorId)
                    .ToList();
                await mediator.Send(new CreateVehicleModelCommand
                {
                    ModelName = row.Get("ModelName")!.Trim(),
                    Description = row.Get("Description")?.Trim() ?? "",
                    ColorIds = colorIds,
                    CreatedBy = context.UserId
                });
            }
            return rows.Count;
        }
    }

    public sealed class PricesImportProcessor : IExcelImportProcessor
    {
        public string Key => ExcelImportKeys.Prices;
        public string TemplateFileName => "import_prices_sample.xlsx";
        public string DataSheetName => "Prices";
        public IReadOnlyList<string> DataHeaders => new[]
        {
            "ModelName", "ColorName", "ApplyForAllColors", "Month", "Year", "Price", "EffectiveFrom", "EffectiveTo", "Notes"
        };
        public IReadOnlyList<IReadOnlyList<object?>> ExampleRows => new[]
        {
            new List<object?> { "Nexus E5", "Pearl White", "No", DateTime.Now.Month, DateTime.Now.Year, 85000m,
                new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1), new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)), "" }
        };

        public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetLookupsAsync(ExcelImportContext context)
        {
            var models = await ExcelImportLookupHelper.GetModelsAsync(context);
            var colors = await ExcelImportLookupHelper.GetColorsAsync(context);
            return new Dictionary<string, IReadOnlyList<string>>
            {
                ["ModelName"] = models.Select(m => m.ModelName).ToList(),
                ["ColorName"] = colors.Select(c => c.ColorName).ToList(),
                ["ApplyForAllColors"] = new[] { "Yes", "No" }
            };
        }

        public async Task<IReadOnlyList<ExcelImportError>> ValidateAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var errors = new List<ExcelImportError>();
            var models = await ExcelImportLookupHelper.GetModelsAsync(context);
            var colors = await ExcelImportLookupHelper.GetColorsAsync(context);
            foreach (var row in rows)
            {
                var modelName = ExcelImportValidationHelper.Require(row, "ModelName", errors);
                ExcelImportValidationHelper.TryYesNo(row, "ApplyForAllColors", errors, out var allColors);
                ExcelImportValidationHelper.TryInt(row, "Month", errors, out var month);
                ExcelImportValidationHelper.TryInt(row, "Year", errors, out var year);
                ExcelImportValidationHelper.TryDecimal(row, "Price", errors, out var price);
                ExcelImportValidationHelper.TryDate(row, "EffectiveFrom", errors, out var from);
                ExcelImportValidationHelper.TryDate(row, "EffectiveTo", errors, out var to);
                if (price <= 0) errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "Price", Message = "Price must be greater than zero." });
                if (to < from) errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "EffectiveTo", Message = "EffectiveTo must be on or after EffectiveFrom." });
                if (month is < 1 or > 12) errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "Month", Message = "Month must be 1–12." });
                if (ExcelImportLookupHelper.FindModel(models, modelName) == null)
                    errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "ModelName", Message = $"Unknown model '{modelName}'." });
                if (!allColors)
                {
                    var colorName = ExcelImportValidationHelper.Require(row, "ColorName", errors);
                    if (colorName != null && ExcelImportLookupHelper.FindColor(colors, colorName) == null)
                        errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "ColorName", Message = $"Unknown color '{colorName}'." });
                }
            }
            return errors;
        }

        public async Task<int> InsertAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var mediator = context.Services.GetRequiredService<IMediator>();
            var models = await ExcelImportLookupHelper.GetModelsAsync(context);
            var colors = await ExcelImportLookupHelper.GetColorsAsync(context);
            foreach (var row in rows)
            {
                ExcelImportValidationHelper.TryYesNo(row, "ApplyForAllColors", new List<ExcelImportError>(), out var allColors);
                var model = ExcelImportLookupHelper.FindModel(models, row.Get("ModelName"))!;
                var color = allColors ? colors.First() : ExcelImportLookupHelper.FindColor(colors, row.Get("ColorName"))!;
                await mediator.Send(new CreateVehiclePriceCommand
                {
                    ModelId = model.ModelId,
                    ColorId = color.ColorId,
                    ApplyForAllColors = allColors,
                    Month = int.Parse(row.Get("Month")!),
                    Year = int.Parse(row.Get("Year")!),
                    EffectiveFrom = DateTime.Parse(row.Get("EffectiveFrom")!).Date,
                    EffectiveTo = DateTime.Parse(row.Get("EffectiveTo")!).Date,
                    Price = decimal.Parse(row.Get("Price")!),
                    Notes = row.Get("Notes")?.Trim(),
                    CreatedBy = context.UserId
                });
            }
            return rows.Count;
        }
    }

    public sealed class CommissionRatesImportProcessor : IExcelImportProcessor
    {
        public string Key => ExcelImportKeys.CommissionRates;
        public string TemplateFileName => "import_commission_rates_sample.xlsx";
        public string DataSheetName => "Commission Rates";
        public IReadOnlyList<string> DataHeaders => new[] { "ModelName", "CommissionAmount", "EffectiveFrom", "EffectiveTo", "Notes" };
        public IReadOnlyList<IReadOnlyList<object?>> ExampleRows => new[]
        {
            new List<object?> { "Nexus E5", 2500m, new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)), "" }
        };

        public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetLookupsAsync(ExcelImportContext context)
        {
            var models = await ExcelImportLookupHelper.GetModelsAsync(context);
            return new Dictionary<string, IReadOnlyList<string>>
            {
                ["ModelName"] = models.Select(m => m.ModelName).ToList()
            };
        }

        public async Task<IReadOnlyList<ExcelImportError>> ValidateAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var errors = new List<ExcelImportError>();
            var models = await ExcelImportLookupHelper.GetModelsAsync(context);
            foreach (var row in rows)
            {
                var modelName = ExcelImportValidationHelper.Require(row, "ModelName", errors);
                ExcelImportValidationHelper.TryDecimal(row, "CommissionAmount", errors, out var amt);
                ExcelImportValidationHelper.TryDate(row, "EffectiveFrom", errors, out var from);
                ExcelImportValidationHelper.TryDate(row, "EffectiveTo", errors, out var to);
                if (amt <= 0) errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "CommissionAmount", Message = "Commission must be greater than zero." });
                if (to < from) errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "EffectiveTo", Message = "EffectiveTo must be on or after EffectiveFrom." });
                if (ExcelImportLookupHelper.FindModel(models, modelName) == null)
                    errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "ModelName", Message = $"Unknown model '{modelName}'." });
            }
            return errors;
        }

        public async Task<int> InsertAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var mediator = context.Services.GetRequiredService<IMediator>();
            var models = await ExcelImportLookupHelper.GetModelsAsync(context);
            foreach (var row in rows)
            {
                var model = ExcelImportLookupHelper.FindModel(models, row.Get("ModelName"))!;
                await mediator.Send(new CreateCommissionRateCommand
                {
                    ModelId = model.ModelId,
                    CommissionAmount = decimal.Parse(row.Get("CommissionAmount")!),
                    EffectiveFrom = DateTime.Parse(row.Get("EffectiveFrom")!).Date,
                    EffectiveTo = DateTime.Parse(row.Get("EffectiveTo")!).Date,
                    Notes = row.Get("Notes")?.Trim(),
                    CreatedBy = context.UserId
                });
            }
            return rows.Count;
        }
    }

    public sealed class StaffUsersImportProcessor : IExcelImportProcessor
    {
        public string Key => ExcelImportKeys.StaffUsers;
        public string TemplateFileName => "import_staff_users_sample.xlsx";
        public string DataSheetName => "Staff Users";
        public IReadOnlyList<string> DataHeaders => new[] { "FullName", "Username", "Password", "RoleName", "DealershipCode", "Email", "PhoneNumber" };
        public IReadOnlyList<IReadOnlyList<object?>> ExampleRows => new[]
        {
            new List<object?> { "Finance User", "finance.salem", "ChangeMe@123", "Salem Finance Manager", "KRS_SALEM", "finance@krs.com", "9876500000" }
        };

        public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetLookupsAsync(ExcelImportContext context)
        {
            var dealerships = await ExcelImportLookupHelper.GetDealershipsAsync(context);
            var roles = await GetAssignableRolesAsync(context);
            return new Dictionary<string, IReadOnlyList<string>>
            {
                ["RoleName"] = roles.Select(r => r.RoleName).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList(),
                ["DealershipCode"] = dealerships.Select(d => d.DealershipCode).ToList()
            };
        }

        public async Task<IReadOnlyList<ExcelImportError>> ValidateAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var errors = new List<ExcelImportError>();
            var dealerships = await ExcelImportLookupHelper.GetDealershipsAsync(context);
            var roles = await GetAssignableRolesAsync(context);
            var uow = context.Services.GetRequiredService<IUnitOfWork>();
            var users = (await uow.Users.GetAllAsync()).ToList();
            var seenUser = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                ExcelImportValidationHelper.Require(row, "FullName", errors);
                var username = ExcelImportValidationHelper.Require(row, "Username", errors);
                ExcelImportValidationHelper.Require(row, "Password", errors);
                var roleRaw = ExcelImportValidationHelper.Require(row, "RoleName", errors);
                var dealerCode = ExcelImportValidationHelper.Require(row, "DealershipCode", errors);
                var dealer = dealerCode != null ? ExcelImportLookupHelper.FindDealership(dealerships, dealerCode) : null;
                if (dealerCode != null && dealer == null)
                    errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "DealershipCode", Message = $"Unknown dealership '{dealerCode}'." });
                if (roleRaw != null && dealer != null && !TryResolveRoleId(roleRaw, dealer.DealershipId, roles, out _))
                    errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "RoleName", Message = $"No active role '{roleRaw}' for dealership '{dealerCode}'." });
                if (username != null)
                {
                    ExcelImportValidationHelper.DuplicateInFile(row, username, seenUser, "Username", errors);
                    if (users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                        errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = "Username", Message = $"Username '{username}' already exists." });
                }
            }
            return errors;
        }

        public async Task<int> InsertAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context)
        {
            var mediator = context.Services.GetRequiredService<IMediator>();
            var dealerships = await ExcelImportLookupHelper.GetDealershipsAsync(context);
            var roles = await GetAssignableRolesAsync(context);
            foreach (var row in rows)
            {
                var dealer = ExcelImportLookupHelper.FindDealership(dealerships, row.Get("DealershipCode"))!;
                TryResolveRoleId(row.Get("RoleName")!, dealer.DealershipId, roles, out var roleId);
                await mediator.Send(new CreateStaffUserCommand
                {
                    FullName = row.Get("FullName")!.Trim(),
                    Username = row.Get("Username")!.Trim(),
                    Password = row.Get("Password")!,
                    RoleId = roleId,
                    DealershipId = dealer.DealershipId,
                    Email = row.Get("Email")?.Trim(),
                    PhoneNumber = row.Get("PhoneNumber")?.Trim(),
                    CreatedBy = context.UserId
                });
            }
            return rows.Count;
        }

        private static async Task<IReadOnlyList<Domain.Entities.Role>> GetAssignableRolesAsync(ExcelImportContext context)
        {
            var uow = context.Services.GetRequiredService<IUnitOfWork>();
            return (await uow.Roles.GetAllAsync())
                .Where(r => r.IsActive && !r.IsSystemRole && r.DealershipId.HasValue)
                .ToList();
        }

        private static bool TryResolveRoleId(string raw, int dealershipId, IReadOnlyList<Domain.Entities.Role> roles, out int roleId)
        {
            roleId = 0;
            var dealerRoles = roles.Where(r => r.DealershipId == dealershipId).ToList();
            var match = dealerRoles.FirstOrDefault(r => r.RoleName.Equals(raw.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                roleId = match.RoleId;
                return true;
            }

            string? template = null;
            if (raw.Contains("Finance", StringComparison.OrdinalIgnoreCase))
                template = RoleTemplateCodes.FinanceManager;
            else if (raw.Contains("Branch", StringComparison.OrdinalIgnoreCase) || raw.Contains("Manager", StringComparison.OrdinalIgnoreCase))
                template = RoleTemplateCodes.Manager;
            else if (raw.Contains("Insurance", StringComparison.OrdinalIgnoreCase) || raw.Contains("RTO", StringComparison.OrdinalIgnoreCase))
                template = RoleTemplateCodes.InsuranceRtoManager;

            if (template != null)
            {
                match = dealerRoles.FirstOrDefault(r => string.Equals(r.RoleTemplateCode, template, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    roleId = match.RoleId;
                    return true;
                }
            }

            return false;
        }
    }
}
