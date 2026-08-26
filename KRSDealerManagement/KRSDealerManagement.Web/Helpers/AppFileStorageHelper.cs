namespace KRSDealerManagement.Web.Helpers
{
    /// <summary>
    /// Single upload root: {ContentRoot}/Files/{section}/...
    /// Legacy files under wwwroot/Files are still readable until migrated.
    /// </summary>
    public static class AppFileStorageHelper
    {
        public const string RootFolder = "Files";

        public static class Sections
        {
            public const string Payment = "Payment";
            public const string VehicleBooking = "vehicle_booking";
            public const string InsuranceInvoice = "Insurance_Invoice";
            public const string Import = "Import";
        }

        public static string ToRelativePath(string section, string dateFolder, string fileName)
            => $"{RootFolder}/{section}/{dateFolder}/{fileName}".Replace('\\', '/');

        public static string EnsureSectionDayFolder(IWebHostEnvironment env, string section, string dateFolder)
        {
            var dir = Path.Combine(env.ContentRootPath, RootFolder, section, dateFolder);
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static bool TryResolveAbsolute(IWebHostEnvironment env, string? relativePath, out string absolutePath)
        {
            absolutePath = "";
            if (string.IsNullOrWhiteSpace(relativePath) || relativePath.Contains(".."))
                return false;

            var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);

            if (TryResolveUnderRoot(env.ContentRootPath, normalized, out absolutePath))
                return true;

            if (!string.IsNullOrEmpty(env.WebRootPath)
                && TryResolveUnderRoot(env.WebRootPath, normalized, out absolutePath))
                return true;

            return false;
        }

        public static bool FileExists(IWebHostEnvironment env, string? relativePath)
            => TryResolveAbsolute(env, relativePath, out _);

        private static bool TryResolveUnderRoot(string root, string normalizedRelative, out string absolutePath)
        {
            absolutePath = Path.GetFullPath(Path.Combine(root, normalizedRelative));
            var rootFull = Path.GetFullPath(root);
            if (!absolutePath.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            {
                absolutePath = "";
                return false;
            }

            return File.Exists(absolutePath);
        }
    }
}
