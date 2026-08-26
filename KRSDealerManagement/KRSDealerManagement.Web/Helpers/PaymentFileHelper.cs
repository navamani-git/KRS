using Microsoft.AspNetCore.Http;

namespace KRSDealerManagement.Web.Helpers
{
    /// <summary>
    /// Saves payment proofs under Files/Payment/YYYY_MM_DD/{ticks}_{safeFileName}
    /// </summary>
    public static class PaymentFileHelper
    {
        private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        public static async Task<string> SaveAsync(IFormFile file, IWebHostEnvironment env)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("File is empty.");

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";
            if (!AllowedExtensions.Contains(ext))
                throw new InvalidOperationException("Allowed file types: PDF, JPG, PNG, WEBP, GIF.");

            var dayFolder = DateTime.Now.ToString("yyyy_MM_dd");
            var absoluteDir = AppFileStorageHelper.EnsureSectionDayFolder(env, AppFileStorageHelper.Sections.Payment, dayFolder);

            var safeName = Path.GetFileName(file.FileName).Replace(" ", "_");
            foreach (var c in Path.GetInvalidFileNameChars())
                safeName = safeName.Replace(c, '_');

            var storedName = $"{DateTime.Now.Ticks}_{safeName}";
            var absolutePath = Path.Combine(absoluteDir, storedName);

            await using var stream = new FileStream(absolutePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return AppFileStorageHelper.ToRelativePath(AppFileStorageHelper.Sections.Payment, dayFolder, storedName);
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

        public static bool CanViewInline(string absolutePath)
        {
            var ext = Path.GetExtension(absolutePath)?.ToLowerInvariant() ?? "";
            return ext is ".pdf" or ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif";
        }
    }
}
