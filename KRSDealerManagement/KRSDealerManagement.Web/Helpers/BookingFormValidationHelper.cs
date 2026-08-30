using System.Text.RegularExpressions;
using KRSDealerManagement.Shared.Constants;
using Microsoft.AspNetCore.Http;

namespace KRSDealerManagement.Web.Helpers
{
    public static class BookingFormValidationHelper
    {
        private static readonly Regex CapitalLettersOnly = new(@"^[A-Z\s\.]+$", RegexOptions.Compiled);
        private static readonly Regex TenDigitMobile = new(@"^\d{10}$", RegexOptions.Compiled);
        private static readonly Regex LowercaseEmail = new(@"^[a-z0-9._%+\-]+@[a-z0-9.\-]+\.[a-z]{2,}$", RegexOptions.Compiled);
        private static readonly Regex EAadhaarPassword = new(@"^[A-Z0-9]+$", RegexOptions.Compiled);

        public static string NormalizeCustomerName(string value) => value.Trim().ToUpperInvariant();
        public static string NormalizeEmail(string value) => value.Trim().ToLowerInvariant();

        public static string? ValidateCustomerName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Customer Name (Exactly Same As in E-Aadhaar) is required.";
            var normalized = NormalizeCustomerName(value);
            if (!CapitalLettersOnly.IsMatch(normalized))
                return "Customer Name: Type Only Capital Letters.";
            return null;
        }

        public static string? ValidateCustomerMobile(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Customer Mobile (Aadhaar Linked) is required.";
            var digits = value.Trim();
            if (!TenDigitMobile.IsMatch(digits))
                return "Customer Mobile (Aadhaar Linked): Enter 10 Digit Only (Ex: 9897960000).";
            return null;
        }

        public static string? ValidateAlternativeMobile(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Alternative Customer Mobile is required.";
            var digits = value.Trim();
            if (!TenDigitMobile.IsMatch(digits))
                return "Alternative Customer Mobile: Enter 10 Digit Only (Ex: 9897960000).";
            return null;
        }

        public static string? ValidateCustomerEmail(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Customer E-Mail ID is required.";
            var normalized = NormalizeEmail(value);
            if (!LowercaseEmail.IsMatch(normalized))
                return "Customer E-Mail ID: Type Only Small Letters.";
            return null;
        }

        public static string? ValidateEAadhaarPassword(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "E-Aadhaar Password is required.";
            var trimmed = value.Trim();
            if (!EAadhaarPassword.IsMatch(trimmed))
                return "E-Aadhaar Password: Type Only Capital Letters & Numbers.";
            return null;
        }

        public static string? ValidateNomineeRelationship(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Nominee's Relationship With Customer is required.";
            if (!BookingFormConstants.NomineeRelationships.Contains(value.Trim(), StringComparer.Ordinal))
                return "Nominee's Relationship With Customer: select a valid option.";
            return null;
        }

        public static string? ValidatePdfFile(IFormFile? file, string fieldLabel, long maxBytes, bool required)
        {
            if (file == null || file.Length == 0)
                return required ? $"{fieldLabel} is required." : null;

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";
            if (ext != ".pdf")
                return $"{fieldLabel}: Upload 1 supported file: PDF.";

            if (file.Length > maxBytes)
            {
                var maxMb = maxBytes / (1024 * 1024);
                return $"{fieldLabel}: Maximum file size is {maxMb} MB.";
            }

            return null;
        }

        public static string? ValidateImageFile(IFormFile? file, string fieldLabel, bool required)
        {
            if (file == null || file.Length == 0)
                return required ? $"{fieldLabel} is required." : null;

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".gif"))
                return $"{fieldLabel}: Upload 1 supported file: image.";

            if (file.Length > BookingFormConstants.MaxImageBytes)
                return $"{fieldLabel}: Maximum file size is 10 MB.";

            return null;
        }

        public static string? ValidateCreateBooking(
            string? customerName,
            string? customerMobile,
            string? alternativeMobile,
            string? customerEmail,
            string? eAadhaarPassword,
            string? nomineeRelationship,
            bool isCompanyBooking,
            IFormFile? eAadhaarFile,
            IFormFile? documentFile,
            IFormFile? gstCertificateFile,
            IFormFile? customerPhoto,
            IFormFile? chassisPhoto,
            IFormFile? customerSign)
        {
            return FirstError(
                ValidateCustomerName(customerName),
                ValidateCustomerMobile(customerMobile),
                ValidateAlternativeMobile(alternativeMobile),
                ValidateCustomerEmail(customerEmail),
                ValidateEAadhaarPassword(eAadhaarPassword),
                ValidateNomineeRelationship(nomineeRelationship),
                ValidatePdfFile(eAadhaarFile, "E-Aadhaar (PDF)", BookingFormConstants.MaxEAadhaarPdfBytes, required: true),
                ValidatePdfFile(documentFile, "Document (PDF)", BookingFormConstants.MaxDocumentPdfBytes, required: true),
                isCompanyBooking
                    ? ValidatePdfFile(gstCertificateFile, "GST Certificate", BookingFormConstants.MaxGstPdfBytes, required: true)
                    : null,
                ValidateImageFile(customerPhoto, "Customer Photo", required: true),
                ValidateImageFile(chassisPhoto, "Chassis Number", required: true),
                ValidateImageFile(customerSign, isCompanyBooking ? "Company Seal with Sign" : "Customer Sign", required: true));
        }

        public static string? ValidateEditBooking(
            string? customerName,
            string? customerMobile,
            string? alternativeMobile,
            string? customerEmail,
            string? eAadhaarPassword,
            string? nomineeRelationship,
            bool isCompanyBooking,
            bool hasGstOnRecord,
            IFormFile? eAadhaarFile,
            IFormFile? documentFile,
            IFormFile? gstCertificateFile,
            IFormFile? customerPhoto,
            IFormFile? chassisPhoto,
            IFormFile? customerSign)
        {
            var gstRequired = isCompanyBooking && !hasGstOnRecord;
            return FirstError(
                ValidateCustomerName(customerName),
                ValidateCustomerMobile(customerMobile),
                ValidateAlternativeMobile(alternativeMobile),
                ValidateCustomerEmail(customerEmail),
                ValidateEAadhaarPassword(eAadhaarPassword),
                ValidateNomineeRelationship(nomineeRelationship),
                ValidatePdfFile(eAadhaarFile, "E-Aadhaar (PDF)", BookingFormConstants.MaxEAadhaarPdfBytes, required: false),
                ValidatePdfFile(documentFile, "Document (PDF)", BookingFormConstants.MaxDocumentPdfBytes, required: false),
                gstRequired
                    ? ValidatePdfFile(gstCertificateFile, "GST Certificate", BookingFormConstants.MaxGstPdfBytes, required: true)
                    : ValidatePdfFile(gstCertificateFile, "GST Certificate", BookingFormConstants.MaxGstPdfBytes, required: false),
                ValidateImageFile(customerPhoto, "Customer Photo", required: false),
                ValidateImageFile(chassisPhoto, "Chassis Number", required: false),
                ValidateImageFile(customerSign, isCompanyBooking ? "Company Seal with Sign" : "Customer Sign", required: false));
        }

        private static string? FirstError(params string?[] errors)
            => errors.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e));
    }
}
