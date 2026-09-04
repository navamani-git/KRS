namespace KRSDealerManagement.Web.Helpers
{
    /// <summary>
    /// Upload root: {ContentRoot}/Files/{section}/...
    /// Example: KRSDealerManagement.Web/Files/Warranty/2026_09_04/file.pdf
    ///
    /// Do not save uploads under wwwroot/Files. That folder is legacy-only;
    /// files there are copied into ContentRoot/Files on startup when missing.
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
            public const string Warranty = "Warranty";
        }

        public static string ToRelativePath(string section, string dateFolder, string fileName)
            => $"{RootFolder}/{section}/{dateFolder}/{fileName}".Replace('\\', '/');

        public static string EnsureSectionDayFolder(IWebHostEnvironment env, string section, string dateFolder)
        {
            var dir = Path.Combine(env.ContentRootPath, RootFolder, section, dateFolder);
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// Copies legacy uploads from wwwroot/Files into ContentRoot/Files when the target file does not exist.
        /// </summary>
        public static int MigrateLegacyWwwrootFiles(IWebHostEnvironment env)
        {
            if (string.IsNullOrWhiteSpace(env.WebRootPath))
                return 0;

            var legacyRoot = Path.Combine(env.WebRootPath, RootFolder);
            if (!Directory.Exists(legacyRoot))
                return 0;

            var targetRoot = Path.Combine(env.ContentRootPath, RootFolder);
            Directory.CreateDirectory(targetRoot);

            var migrated = 0;
            foreach (var sourceFile in Directory.EnumerateFiles(legacyRoot, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(legacyRoot, sourceFile);
                var targetFile = Path.Combine(targetRoot, relative);
                if (File.Exists(targetFile))
                    continue;

                var targetDir = Path.GetDirectoryName(targetFile);
                if (!string.IsNullOrWhiteSpace(targetDir))
                    Directory.CreateDirectory(targetDir);

                File.Copy(sourceFile, targetFile);
                migrated++;
            }

            return migrated;
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
