using Microsoft.AspNetCore.Http;

namespace KRSDealerManagement.Web.Helpers
{
    /// <summary>
    /// Saves payment proofs under Files/Payment/YYYY_MM_DD/{ticks}_{safeFileName}
    /// </summary>
    public static class PaymentFileHelper
    {
        private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        public static async Task<string> SaveAsync(IFormFile file, string webRootOrContentRoot)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("File is empty.");

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";
            if (!AllowedExtensions.Contains(ext))
                throw new InvalidOperationException("Allowed file types: PDF, JPG, PNG, WEBP, GIF.");

            var dayFolder = DateTime.Now.ToString("yyyy_MM_dd");
            var relativeDir = Path.Combine("Files", "Payment", dayFolder);
            var absoluteDir = Path.Combine(webRootOrContentRoot, relativeDir);
            Directory.CreateDirectory(absoluteDir);

            var safeName = Path.GetFileName(file.FileName)
                .Replace(" ", "_");
            foreach (var c in Path.GetInvalidFileNameChars())
                safeName = safeName.Replace(c, '_');

            var storedName = $"{DateTime.Now.Ticks}_{safeName}";
            var absolutePath = Path.Combine(absoluteDir, storedName);

            await using var stream = new FileStream(absolutePath, FileMode.Create);
            await file.CopyToAsync(stream);

            // Store relative path with forward slashes for web/download
            return Path.Combine(relativeDir, storedName).Replace('\\', '/');
        }
    }
}
