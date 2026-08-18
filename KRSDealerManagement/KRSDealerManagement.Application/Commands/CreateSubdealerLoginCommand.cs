using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>Add a login user under an existing subdealer org with menu permissions.</summary>
    public class CreateSubdealerLoginCommand : IRequest<int>
    {
        public int SubDealerId { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public string? DisplayName { get; set; }
        /// <summary>Applied only when this is the first login (creates org wallet).</summary>
        public decimal InitialBalance { get; set; }
        public List<string>? AccessibleMenuKeys { get; set; }
        public int CreatedBy { get; set; }
    }
}
