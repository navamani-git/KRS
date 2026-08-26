using Microsoft.AspNetCore.Http;
using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Web.Helpers
{
    public static class BookingFileHelper
    {
        private static readonly string[] PdfOnly = { ".pdf" };
        private static readonly string[] ImageOnly = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private const long MaxBytes = 10 * 1024 * 1024;

        public static string StorageFolder => AppFileStorageHelper.Sections.VehicleBooking;
        public static string InsuranceInvoiceFolder => AppFileStorageHelper.Sections.InsuranceInvoice;
        public static string StorageFolderPrefix => $"{AppFileStorageHelper.RootFolder}/{StorageFolder}/";
        public static string InsuranceInvoiceFolderPrefix => $"{AppFileStorageHelper.RootFolder}/{InsuranceInvoiceFolder}/";

        public static Task<string> SavePdfAsync(IFormFile file, IWebHostEnvironment env)
            => SaveAsync(file, env, StorageFolder, PdfOnly);

        public static Task<string> SaveImageAsync(IFormFile file, IWebHostEnvironment env)
            => SaveAsync(file, env, StorageFolder, ImageOnly);

        public static Task<string> SaveDocumentAsync(IFormFile file, IWebHostEnvironment env)
            => SaveAsync(file, env, StorageFolder, PdfOnly.Concat(ImageOnly).ToArray());

        public static Task<string> SaveInvoiceDocumentAsync(IFormFile file, IWebHostEnvironment env)
            => SaveAsync(file, env, InsuranceInvoiceFolder, PdfOnly.Concat(ImageOnly).ToArray(), "Invoice");

        public static Task<string> SaveInsuranceDocumentAsync(IFormFile file, IWebHostEnvironment env)
            => SaveAsync(file, env, InsuranceInvoiceFolder, PdfOnly.Concat(ImageOnly).ToArray(), "Insurance");

        private static async Task<string> SaveAsync(
            IFormFile file,
            IWebHostEnvironment env,
            string section,
            string[] allowed,
            string? namePrefix = null)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("File is empty.");
            if (file.Length > MaxBytes)
                throw new InvalidOperationException("Maximum file size is 10 MB.");

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";
            if (!allowed.Contains(ext))
                throw new InvalidOperationException($"Allowed file types: {string.Join(", ", allowed)}.");

            var dayFolder = DateTime.Now.ToString("yyyy_MM_dd");
            var absoluteDir = AppFileStorageHelper.EnsureSectionDayFolder(env, section, dayFolder);

            var storedName = SanitizeFileName(file.FileName);
            if (!string.IsNullOrEmpty(namePrefix))
                storedName = $"{namePrefix}_{storedName}";

            var absolutePath = Path.Combine(absoluteDir, storedName);
            if (File.Exists(absolutePath))
            {
                var name = Path.GetFileNameWithoutExtension(storedName);
                storedName = $"{name}_{DateTime.Now:HHmmssfff}{ext}";
                absolutePath = Path.Combine(absoluteDir, storedName);
            }

            await using var stream = new FileStream(absolutePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return AppFileStorageHelper.ToRelativePath(section, dayFolder, storedName);
        }

        private static string SanitizeFileName(string fileName)
        {
            var safeName = Path.GetFileName(fileName);
            foreach (var c in Path.GetInvalidFileNameChars())
                safeName = safeName.Replace(c, '_');
            return safeName;
        }

        public static bool IsStoredBookingFilePath(string? relativePath) =>
            !string.IsNullOrWhiteSpace(relativePath)
            && (relativePath.StartsWith(StorageFolderPrefix, StringComparison.OrdinalIgnoreCase)
                || relativePath.StartsWith(InsuranceInvoiceFolderPrefix, StringComparison.OrdinalIgnoreCase));

        public static bool BookingContainsFilePath(VehicleBooking booking, string path) =>
            string.Equals(booking.EAadhaarPath, path, StringComparison.OrdinalIgnoreCase)
            || string.Equals(booking.DocumentPath, path, StringComparison.OrdinalIgnoreCase)
            || string.Equals(booking.GstCertificatePath, path, StringComparison.OrdinalIgnoreCase)
            || string.Equals(booking.CustomerPhotoPath, path, StringComparison.OrdinalIgnoreCase)
            || string.Equals(booking.ChassisPhotoPath, path, StringComparison.OrdinalIgnoreCase)
            || string.Equals(booking.CustomerSignPath, path, StringComparison.OrdinalIgnoreCase)
            || string.Equals(booking.FaceVerificationPath, path, StringComparison.OrdinalIgnoreCase)
            || string.Equals(booking.RcImagePath, path, StringComparison.OrdinalIgnoreCase)
            || string.Equals(booking.BoothPhotoPath, path, StringComparison.OrdinalIgnoreCase)
            || string.Equals(booking.SubsidyUndertakingPath, path, StringComparison.OrdinalIgnoreCase)
            || string.Equals(booking.InvoicePath, path, StringComparison.OrdinalIgnoreCase)
            || string.Equals(booking.InsurancePath, path, StringComparison.OrdinalIgnoreCase);

        public static string ResolvePath(IWebHostEnvironment env, string? relativePath)
            => AppFileStorageHelper.TryResolveAbsolute(env, relativePath, out var full) ? full : "";

        public static bool IsFileAvailable(IWebHostEnvironment env, string? relativePath)
            => AppFileStorageHelper.FileExists(env, relativePath);

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
