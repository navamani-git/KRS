namespace KRSDealerManagement.Application.DTOs
{
    public class VehicleAgingRowDto
    {
        public int VehicleId { get; set; }
        public int? VehicleBookingId { get; set; }
        public string ChassisNumber { get; set; } = "";
        public string ModelName { get; set; } = "";
        public string ColorName { get; set; } = "";
        public int SubdealerId { get; set; }
        public string SubdealerName { get; set; } = "";
        public DateTime? PurchaseDate { get; set; }
        public DateTime? BookedDate { get; set; }
        public int? BookedAging { get; set; }
        public DateTime? PaperReceivedDate { get; set; }
        public int? PaperReceivedAging { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public int? InvoiceAging { get; set; }
        public DateTime? InsuranceDate { get; set; }
        public int? InsuranceAging { get; set; }
        public DateTime? AgentDate { get; set; }
        public int? AgentAging { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public int? RegistrationAging { get; set; }
        public int? SubsidyAging { get; set; }
        public int? SubsidyDocumentsAging { get; set; }
    }
}
