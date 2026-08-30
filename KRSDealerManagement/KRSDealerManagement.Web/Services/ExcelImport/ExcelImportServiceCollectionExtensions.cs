using KRSDealerManagement.Web.Services.ExcelImport;
using KRSDealerManagement.Web.Services.ExcelImport.Processors;

namespace KRSDealerManagement.Web.Services.ExcelImport
{
    public static class ExcelImportServiceCollectionExtensions
    {
        public static IServiceCollection AddExcelImport(this IServiceCollection services)
        {
            services.AddScoped<ExcelImportService>();
            services.AddScoped<IExcelImportProcessor, RtoLocationsImportProcessor>();
            services.AddScoped<IExcelImportProcessor, DocumentTypesImportProcessor>();
            services.AddScoped<IExcelImportProcessor, FinanceNamesImportProcessor>();
            services.AddScoped<IExcelImportProcessor, DealershipsImportProcessor>();
            services.AddScoped<IExcelImportProcessor, PaymentTypesImportProcessor>();
            services.AddScoped<IExcelImportProcessor, VehicleColorsImportProcessor>();
            services.AddScoped<IExcelImportProcessor, StatusLookupsImportProcessor>();
            services.AddScoped<IExcelImportProcessor, VehicleModelsImportProcessor>();
            services.AddScoped<IExcelImportProcessor, PricesImportProcessor>();
            services.AddScoped<IExcelImportProcessor, CommissionRatesImportProcessor>();
            services.AddScoped<IExcelImportProcessor, StaffUsersImportProcessor>();
            services.AddScoped<IExcelImportProcessor, SubdealersImportProcessor>();
            services.AddScoped<IExcelImportProcessor, SubdealerAccountsImportProcessor>();
            services.AddScoped<IExcelImportProcessor, OrdersSubdealerImportProcessor>();
            services.AddScoped<IExcelImportProcessor, OrdersForSubdealerImportProcessor>();
            services.AddScoped<IExcelImportProcessor, VehicleMastersImportProcessor>();
            return services;
        }
    }
}
