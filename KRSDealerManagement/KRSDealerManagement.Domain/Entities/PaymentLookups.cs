namespace KRSDealerManagement.Domain.Entities
{
    public class PaymentType
    {
        public int PaymentTypeId { get; set; }
        public string TypeCode { get; set; } = "";
        public string TypeName { get; set; } = "";
        public bool RequiresFinanceDetails { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Master list of finance companies for payment dropdown.</summary>
    public class FinanceNameMaster
    {
        public int FinanceNameId { get; set; }
        public string FinanceName { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    }
}
