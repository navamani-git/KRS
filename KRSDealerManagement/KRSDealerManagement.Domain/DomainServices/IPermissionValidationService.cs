using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Domain.DomainServices
{
    /// <summary>
    /// Domain service for validating account permissions
    /// </summary>
    public interface IPermissionValidationService
    {
        /// <summary>
        /// Check if account can access specific menu/feature
        /// </summary>
        bool CanAccessMenu(SubdealerAccount account, string menuKey);

        /// <summary>
        /// Check if account can perform specific action on menu
        /// </summary>
        bool CanPerformAction(SubdealerAccount account, string menuKey, string action);

        /// <summary>
        /// Get all accessible menus for account
        /// </summary>
        IEnumerable<AccountPermission> GetAccessibleMenus(SubdealerAccount account);

        /// <summary>
        /// Get all permissions for account
        /// </summary>
        IEnumerable<AccountPermission> GetAllPermissions(int accountId);

        /// <summary>
        /// Validate permission before allowing action
        /// Returns validation result with error message
        /// </summary>
        (bool IsValid, string ErrorMessage) ValidatePermission(SubdealerAccount account, string menuKey, string action);
    }
}
