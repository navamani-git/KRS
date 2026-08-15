using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    /// <summary>
    /// Get all permissions for specific account
    /// </summary>
    public class GetAccountPermissionsQuery : IRequest<IEnumerable<AccountPermissionDto>>
    {
        public int AccountId { get; set; }
        public bool? IsAccessibleOnly { get; set; } // Filter to show only accessible menus
    }
}
