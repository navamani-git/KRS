using MediatR;
using KRSDealerManagement.Shared.Enums;
using KRSDealerManagement.Shared.Results;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>
    /// Login command for user authentication
    /// </summary>
    public class LoginCommand : IRequest<Result<LoginResult>>
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
        public bool RememberMe { get; set; }
    }

    /// <summary>
    /// Login result containing user details
    /// </summary>
    public class LoginResult
    {
        public int UserId { get; set; }
        public required string Username { get; set; }
        public required string FullName { get; set; }
        public int UserRole { get; set; }
        public required string RoleName { get; set; }
        public required string RoleCode { get; set; }
        public int? DealershipId { get; set; }
        public string? DealershipName { get; set; }
        public int? SubDealerId { get; set; }
        public List<string> AccessibleMenuKeys { get; set; } = new();
        public Dictionary<string, MenuAccessLevel> MenuAccess { get; set; } = new();
        public bool IsActive { get; set; }
        public bool CanExport { get; set; } = true;
        public string? QuickActionKeys { get; set; }
        public string? DashboardWidgetKeys { get; set; }
    }
}
