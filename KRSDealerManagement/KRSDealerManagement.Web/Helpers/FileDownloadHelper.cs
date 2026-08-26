using Microsoft.AspNetCore.Mvc;

namespace KRSDealerManagement.Web.Helpers
{
    public static class FileDownloadHelper
    {
        public const string MissingFileMessage =
            "The requested file is no longer available or could not be found on the server.";

        public const string InvalidFileMessage = "Invalid file request.";
        public const string AccessDeniedMessage = "You do not have permission to access this file.";

        public static IActionResult RedirectWithMessage(
            Controller controller,
            string message,
            string fallbackController,
            string fallbackAction = "Index")
        {
            controller.TempData["Error"] = message;

            if (TryGetSameOriginLocalUrl(controller.Request, controller.Request.Headers.Referer.ToString(), out var localUrl))
                return controller.Redirect(localUrl);

            return controller.RedirectToAction(fallbackAction, fallbackController);
        }

        public static IActionResult RedirectMissingFile(
            Controller controller,
            string fallbackController,
            string fallbackAction = "Index",
            string? message = null)
            => RedirectWithMessage(controller, message ?? MissingFileMessage, fallbackController, fallbackAction);

        public static bool TryResolveStoredFile(IWebHostEnvironment env, string? relativePath, out string absolutePath)
            => AppFileStorageHelper.TryResolveAbsolute(env, relativePath, out absolutePath);

        public static bool TryResolveContentRootFile(string contentRoot, string? relativePath, out string absolutePath)
        {
            absolutePath = "";
            if (string.IsNullOrWhiteSpace(relativePath) || relativePath.Contains(".."))
                return false;

            absolutePath = Path.GetFullPath(Path.Combine(
                contentRoot,
                relativePath.Replace('/', Path.DirectorySeparatorChar)));

            var root = Path.GetFullPath(contentRoot);
            if (!absolutePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return false;

            return File.Exists(absolutePath);
        }

        private static bool TryGetSameOriginLocalUrl(HttpRequest request, string? referer, out string localUrl)
        {
            localUrl = "";
            if (string.IsNullOrWhiteSpace(referer))
                return false;

            if (!Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
                return false;

            var origin = $"{request.Scheme}://{request.Host.Value}";
            if (!referer.StartsWith(origin, StringComparison.OrdinalIgnoreCase))
                return false;

            localUrl = refererUri.PathAndQuery;
            return !string.IsNullOrWhiteSpace(localUrl);
        }
    }
}
