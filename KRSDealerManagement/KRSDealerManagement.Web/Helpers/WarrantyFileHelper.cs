using Microsoft.AspNetCore.Http;

namespace KRSDealerManagement.Web.Helpers
{
    public static class WarrantyFileHelper
    {
        public const long MaxFileBytes = 100L * 1024 * 1024;

        private static readonly string[] AllowedExtensions =
        {
            ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp",
            ".mp4", ".mov", ".avi", ".mkv", ".webm", ".m4v"
        };

        public static async Task<string> SaveAsync(IFormFile file, IWebHostEnvironment env)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("File is empty.");
            if (file.Length > MaxFileBytes)
                throw new InvalidOperationException("Maximum file size is 100 MB.");

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";
            if (!AllowedExtensions.Contains(ext))
                throw new InvalidOperationException("Allowed file types: images, PDF, and video (MP4, MOV, WEBM, etc.).");

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
    }
}
