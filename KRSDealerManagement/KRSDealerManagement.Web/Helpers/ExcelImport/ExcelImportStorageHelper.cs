namespace KRSDealerManagement.Web.Helpers.ExcelImport
{
    public static class ExcelImportStorageHelper
    {
        public static string RelativeRoot => $"{AppFileStorageHelper.RootFolder}/{AppFileStorageHelper.Sections.Import}";

        public static async Task<string> SaveUploadedFileAsync(IFormFile file, IWebHostEnvironment env)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("No file uploaded.");

            var ext = Path.GetExtension(file.FileName);
            if (!string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(ext, ".xls", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only Excel files (.xlsx) are supported.");

            var dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
            var dir = AppFileStorageHelper.EnsureSectionDayFolder(env, AppFileStorageHelper.Sections.Import, dateFolder);

            var baseName = Path.GetFileNameWithoutExtension(file.FileName);
            foreach (var c in Path.GetInvalidFileNameChars())
                baseName = baseName.Replace(c, '_');

            var fileName = $"{baseName}_{DateTime.Now:HHmmssfff}{ext}";
            var fullPath = Path.Combine(dir, fileName);

            await using var stream = File.Create(fullPath);
            await file.CopyToAsync(stream);

            return AppFileStorageHelper.ToRelativePath(AppFileStorageHelper.Sections.Import, dateFolder, fileName);
        }

        public static string ResolveFullPath(IWebHostEnvironment env, string relativePath)
        {
            if (!AppFileStorageHelper.TryResolveAbsolute(env, relativePath, out var full))
                throw new FileNotFoundException("Uploaded import file could not be found.", relativePath);
            return full;
        }
    }
}
