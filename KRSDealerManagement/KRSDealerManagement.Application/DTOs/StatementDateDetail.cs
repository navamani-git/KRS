namespace KRSDealerManagement.Application.DTOs
{
    public class StatementDateDetail
    {
        public required string Label { get; set; }
        public DateTime Value { get; set; }
        public bool DateOnly { get; set; }
    }
}
