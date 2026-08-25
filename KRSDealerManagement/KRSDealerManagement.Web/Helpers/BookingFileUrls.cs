using KRSDealerManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace KRSDealerManagement.Web.Helpers
{
    public static class BookingFileUrls
    {
        public static string? View(IUrlHelper url, IQueryStringCrypto crypto, string? path) =>
            Action(url, crypto, "ViewFile", path);

        public static string? Download(IUrlHelper url, IQueryStringCrypto crypto, string? path) =>
            Action(url, crypto, "Download", path);

        private static string? Action(IUrlHelper url, IQueryStringCrypto crypto, string action, string? path) =>
            string.IsNullOrWhiteSpace(path)
                ? null
                : QueryStringUrlHelper.EncryptedAction(url, crypto, action, new { path }, "VehicleBookings");
    }
}
