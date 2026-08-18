using Microsoft.AspNetCore.Http;

namespace KRSDealerManagement.Web.Helpers
{
    public static class BookingFileHelper
    {
        private static readonly string[] PdfOnly = { ".pdf" };
        private static readonly string[] ImageOnly = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private const long MaxBytes = 10 * 1024 * 1024;
        private const string Folder = "vehicle_booking";

        public static async Task<string> SavePdfAsync(IFormFile file, string webRoot)
            => await SaveAsync(file, webRoot, PdfOnly);

        public static async Task<string> SaveImageAsync(IFormFile file, string webRoot)
            => await SaveAsync(file, webRoot, ImageOnly);

        private static async Task<string> SaveAsync(IFormFile file, string webRoot, string[] allowed)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("File is empty.");
            if (file.Length > MaxBytes)
                throw new InvalidOperationException("Maximum file size is 10 MB.");

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";
            if (!allowed.Contains(ext))
                throw new InvalidOperationException($"Allowed file types: {string.Join(", ", allowed)}.");

            var dayFolder = DateTime.Now.ToString("yyyy_MM_dd");
            var relativeDir = Path.Combine("Files", Folder, dayFolder);
            var absoluteDir = Path.Combine(webRoot, relativeDir);
            Directory.CreateDirectory(absoluteDir);

            var storedName = SanitizeFileName(file.FileName);
            var absolutePath = Path.Combine(absoluteDir, storedName);
            if (File.Exists(absolutePath))
            {
                var name = Path.GetFileNameWithoutExtension(storedName);
                storedName = $"{name}_{DateTime.Now:HHmmssfff}{ext}";
                absolutePath = Path.Combine(absoluteDir, storedName);
            }

            await using var stream = new FileStream(absolutePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return Path.Combine(relativeDir, storedName).Replace('\\', '/');
        }

        private static string SanitizeFileName(string fileName)
        {
            var safeName = Path.GetFileName(fileName);
            foreach (var c in Path.GetInvalidFileNameChars())
                safeName = safeName.Replace(c, '_');
            return safeName;
        }

        public static string ResolvePath(string webRoot, string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return "";
            var full = Path.GetFullPath(Path.Combine(webRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            var root = Path.GetFullPath(webRoot);
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return "";
            return File.Exists(full) ? full : "";
        }

        public static string GetContentType(string absolutePath)
        {
            var ext = Path.GetExtension(absolutePath)?.ToLowerInvariant() ?? "";
            return ext switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }
    }
}
