namespace KRSDealerManagement.Shared.Constants
{
    public static class BookingFormConstants
    {
        public const long MaxEAadhaarPdfBytes = 10 * 1024 * 1024;
        public const long MaxDocumentPdfBytes = 1 * 1024 * 1024;
        public const long MaxGstPdfBytes = 1 * 1024 * 1024;
        public const long MaxImageBytes = 10 * 1024 * 1024;

        public static readonly IReadOnlyList<string> NomineeRelationships = new[]
        {
            "Customer's Spouse",
            "Customer's Mother",
            "Customer's Father",
            "Customer's Son",
            "Customer's Daughter",
            "Customer's Brother",
            "Customer's Sister"
        };
    }
}
