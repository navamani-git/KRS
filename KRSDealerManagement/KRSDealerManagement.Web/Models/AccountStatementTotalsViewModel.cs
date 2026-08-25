namespace KRSDealerManagement.Web.Models
{
    public class AccountStatementTotalsViewModel
    {
        public decimal TotalApproved { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public int TransactionCount { get; set; }
    }
}
