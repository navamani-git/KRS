using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Web.Helpers.ExcelImport;
using KRSDealerManagement.Web.Models;
using KRSDealerManagement.Web.Services.ExcelImport;

namespace KRSDealerManagement.Web.Services.ExcelImport
{
    public class ExcelImportService
    {
        private readonly IEnumerable<IExcelImportProcessor> _processors;
        private readonly IWebHostEnvironment _env;

        public ExcelImportService(IEnumerable<IExcelImportProcessor> processors, IWebHostEnvironment env)
        {
            _processors = processors;
            _env = env;
        }

        public IExcelImportProcessor? GetProcessor(string key)
            => _processors.FirstOrDefault(p => p.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

        public byte[] BuildTemplate(string key, ExcelImportContext context)
        {
            var processor = GetProcessor(key)
                ?? throw new InvalidOperationException($"Unknown import type: {key}");

            var lookups = processor.GetLookupsAsync(context).GetAwaiter().GetResult();
            return ExcelImportWorkbookHelper.BuildTemplate(
                processor.DataSheetName,
                processor.DataHeaders,
                processor.ExampleRows,
                lookups);
        }

        public async Task<ExcelImportResult> ImportAsync(string key, IFormFile file, ExcelImportContext context)
        {
            var processor = GetProcessor(key)
                ?? throw new InvalidOperationException($"Unknown import type: {key}");

            var result = new ExcelImportResult();
            try
            {
                var webRoot = _env.WebRootPath ?? _env.ContentRootPath;
                result.SavedRelativePath = await ExcelImportStorageHelper.SaveUploadedFileAsync(file, webRoot);

                var fullPath = ExcelImportStorageHelper.ResolveFullPath(webRoot, result.SavedRelativePath);
                List<ExcelImportRow> rows;
                await using (var stream = File.OpenRead(fullPath))
                    rows = ExcelImportWorkbookHelper.ParseDataRows(stream);

                if (rows.Count == 0)
                {
                    result.Errors.Add(new ExcelImportError
                    {
                        RowNumber = 0,
                        Message = "No data rows found. Fill in rows below the example rows in the Data sheet."
                    });
                    return result;
                }

                var errors = (await processor.ValidateAsync(rows, context)).ToList();
                if (errors.Any())
                {
                    result.Errors = errors;
                    return result;
                }

                result.InsertedCount = await processor.InsertAsync(rows, context);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ExcelImportError { RowNumber = 0, Message = ex.Message });
            }

            return result;
        }
    }
}
