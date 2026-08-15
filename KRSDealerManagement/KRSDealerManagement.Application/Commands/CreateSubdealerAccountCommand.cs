using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>
    /// Create additional account for subdealer
    /// Will log account creation and permission setup to AuditLog
    /// </summary>
    public class CreateSubdealerAccountCommand : IRequest<int>
    {
        public int SubdealerId { get; set; }
        public required string AccountName { get; set; }
        public required string AccountType { get; set; }
        public string? Description { get; set; }
        public decimal InitialBalance { get; set; }
        public int CreatedBy { get; set; }
    }
}
