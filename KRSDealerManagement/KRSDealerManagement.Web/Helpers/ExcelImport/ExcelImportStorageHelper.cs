namespace KRSDealerManagement.Web.Helpers.ExcelImport
{
    public static class ExcelImportStorageHelper
    {
        public const string RelativeRoot = "Files/Import";

        public static async Task<string> SaveUploadedFileAsync(IFormFile file, string webRootPath)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("No file uploaded.");

            var ext = Path.GetExtension(file.FileName);
            if (!string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(ext, ".xls", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only Excel files (.xlsx) are supported.");

            var dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
            var dir = Path.Combine(webRootPath, RelativeRoot, dateFolder);
            Directory.CreateDirectory(dir);

            var baseName = Path.GetFileNameWithoutExtension(file.FileName);
            foreach (var c in Path.GetInvalidFileNameChars())
                baseName = baseName.Replace(c, '_');

            var fileName = $"{baseName}_{DateTime.Now:HHmmssfff}{ext}";
            var fullPath = Path.Combine(dir, fileName);

            await using var stream = File.Create(fullPath);
            await file.CopyToAsync(stream);

            return Path.Combine(RelativeRoot, dateFolder, fileName).Replace('\\', '/');
        }

        public static string ResolveFullPath(string webRootPath, string relativePath)
            => Path.Combine(webRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
