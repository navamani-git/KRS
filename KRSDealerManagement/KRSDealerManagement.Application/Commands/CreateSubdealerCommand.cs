using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>
    /// Create new subdealer user and main account
    /// Will automatically log user creation to AuditLog
    /// </summary>
    public class CreateSubdealerCommand : IRequest<int>
    {
        public required string SubdealerName { get; set; }
        public required string Email { get; set; }
        public required string Location { get; set; }
        public required string PrimaryPhone { get; set; }
        public string? SecondaryPhone { get; set; }
        public required string SalesRepMobile { get; set; }
        public required string ServiceRepMobile { get; set; }
        public decimal InitialBalance { get; set; }
        /// <summary>Login password set by admin (stored so admin can view later).</summary>
        public required string Password { get; set; }
        /// <summary>Required: which KRS dealership location this subdealer belongs to.</summary>
        public int DealershipId { get; set; }
        /// <summary>Menu keys the subdealer may access. Empty = role default menus.</summary>
        public List<string>? AccessibleMenuKeys { get; set; }
        public int CreatedBy { get; set; } // Admin UserId
    }
}
