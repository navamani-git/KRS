using Microsoft.AspNetCore.Http;

namespace KRSDealerManagement.Web.Helpers
{
    public static class WarrantyFileHelper
    {
        public const long MaxImageBytes = 1L * 1024 * 1024;
        public const long MaxVideoBytes = 10L * 1024 * 1024;
        public const long MaxFileBytes = MaxVideoBytes;

        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp" };
        private static readonly string[] VideoExtensions = { ".mp4", ".mov", ".avi", ".mkv", ".webm", ".m4v" };
        private static readonly string[] AllowedExtensions =
        {
            ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp",
            ".mp4", ".mov", ".avi", ".mkv", ".webm", ".m4v"
        };

        public static async Task<string> SaveAsync(IFormFile file, IWebHostEnvironment env, string? fieldLabel = null)
        {
            var prefix = string.IsNullOrWhiteSpace(fieldLabel) ? string.Empty : fieldLabel.Trim() + ": ";

            if (file == null || file.Length == 0)
                throw new InvalidOperationException(prefix + "File is empty.");

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";
            if (!AllowedExtensions.Contains(ext))
                throw new InvalidOperationException(prefix + "Allowed file types: images, PDF, and video (MP4, MOV, WEBM, etc.).");

            var maxBytes = ImageExtensions.Contains(ext) ? MaxImageBytes
                : VideoExtensions.Contains(ext) ? MaxVideoBytes
                : MaxImageBytes;
            var maxLabel = ImageExtensions.Contains(ext) ? "1 MB" : VideoExtensions.Contains(ext) ? "10 MB" : "1 MB";
            if (file.Length > maxBytes)
                throw new InvalidOperationException(prefix + $"Maximum file size is {maxLabel}.");

            var dayFolder = DateTime.Now.ToString("yyyy_MM_dd");
            var absoluteDir = AppFileStorageHelper.EnsureSectionDayFolder(env, AppFileStorageHelper.Sections.Warranty, dayFolder);

            var safeName = Path.GetFileName(file.FileName).Replace(" ", "_");
            foreach (var c in Path.GetInvalidFileNameChars())
                safeName = safeName.Replace(c, '_');

            var storedName = $"{DateTime.Now.Ticks}_{safeName}";
            var absolutePath = Path.Combine(absoluteDir, storedName);

            await using var stream = new FileStream(absolutePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return AppFileStorageHelper.ToRelativePath(AppFileStorageHelper.Sections.Warranty, dayFolder, storedName);
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
                ".bmp" => "image/bmp",
                ".mp4" => "video/mp4",
                ".mov" => "video/quicktime",
                ".webm" => "video/webm",
                ".avi" => "video/x-msvideo",
                ".mkv" => "video/x-matroska",
                ".m4v" => "video/x-m4v",
                _ => "application/octet-stream"
            };
        }

        public static string GetMediaKind(string filePath)
        {
            var contentType = GetContentType(filePath);
            if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return "image";
            if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
                return "video";
            if (string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
                return "pdf";
            return "unknown";
        }
    }
}
