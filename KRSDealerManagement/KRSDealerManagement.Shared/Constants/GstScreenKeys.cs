namespace KRSDealerManagement.Shared.Constants
{
    /// <summary>Stable keys for GST dropdown default per page. Add a row here when wiring a new screen.</summary>
    public static class GstScreenKeys
    {
        public const string CreditNoteUnit = "credit_note_unit";
        public const string CreditNoteHandling = "credit_note_handling";
        public const string CreditNoteLabour = "credit_note_labour";

        public static IReadOnlyList<(string Key, string Label)> All => new List<(string, string)>
        {
            (CreditNoteUnit, "Warranty Credit Note – Unit Price"),
            (CreditNoteHandling, "Warranty Credit Note – Handling"),
            (CreditNoteLabour, "Warranty Credit Note – Labour"),
        };
    }
}
