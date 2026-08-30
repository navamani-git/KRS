using KRSDealerManagement.Web.Models;

namespace KRSDealerManagement.Web.Services.ExcelImport
{
    public class ExcelImportContext
    {
        public required int UserId { get; init; }
        public int? DealershipScopeId { get; init; }
        public bool IsBranchManager { get; init; }
        public required IServiceProvider Services { get; init; }
    }

    public interface IExcelImportProcessor
    {
        string Key { get; }
        string TemplateFileName { get; }
        string DataSheetName { get; }
        IReadOnlyList<string> DataHeaders { get; }
        IReadOnlyList<IReadOnlyList<object?>> ExampleRows { get; }
        Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetLookupsAsync(ExcelImportContext context);
        Task<IReadOnlyList<ExcelReferenceTable>> GetReferenceTablesAsync(ExcelImportContext context)
            => Task.FromResult<IReadOnlyList<ExcelReferenceTable>>(Array.Empty<ExcelReferenceTable>());
        Task<IReadOnlyList<ExcelImportError>> ValidateAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context);
        Task<int> InsertAsync(IReadOnlyList<ExcelImportRow> rows, ExcelImportContext context);
    }
}
