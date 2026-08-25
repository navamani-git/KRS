namespace KRSDealerManagement.Application.DTOs
{
    public class InvoicePriceChangePreviewDto
    {
        public decimal CurrentVehiclePrice { get; set; }
        public decimal? CatalogPrice { get; set; }
        public decimal Delta { get; set; }
        public bool WouldChange { get; set; }
        public bool HasCatalogPrice { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
