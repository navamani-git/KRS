using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>
    /// Configure permissions for a specific account
    /// Will log each permission change to AuditLog
    /// </summary>
    public class ConfigureAccountPermissionsCommand : IRequest<bool>
    {
        public int AccountId { get; set; }
        public required List<PermissionSetting> Permissions { get; set; }
        public int ConfiguredBy { get; set; }
        public string Remarks { get; set; } = "Permission update";
    }

    /// <summary>
    /// Individual permission setting
    /// </summary>
    public class PermissionSetting
    {
        public required string MenuKey { get; set; }
        public required string MenuName { get; set; }
        public bool IsAccessible { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanApprove { get; set; }
    }
}
