using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Shared.Exceptions;

namespace KRSDealerManagement.Domain.Specifications
{
    /// <summary>
    /// Business rule: Account must have permission for specific action on menu
    /// Used to enforce granular access control
    /// </summary>
    public class HasPermissionSpecification
    {
        private readonly AccountPermission _permission;
        private readonly string _action;

        public HasPermissionSpecification(AccountPermission permission, string action)
        {
            _permission = permission ?? throw new ArgumentNullException(nameof(permission));
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        /// <summary>
        /// Check if specification is satisfied
        /// </summary>
        public bool IsSatisfiedBy()
        {
            return _permission.CanPerformAction(_action);
        }

        /// <summary>
        /// Validate and throw exception if not satisfied
        /// </summary>
        public void Validate()
        {
            if (!IsSatisfiedBy())
            {
                throw new Shared.Exceptions.UnauthorizedAccessException(
                    $"Account does not have permission to {_action} on {_permission.MenuName}"
                );
            }
        }

        /// <summary>
        /// Get validation result with message
        /// </summary>
        public (bool IsValid, string Message) GetValidationResult()
        {
            if (IsSatisfiedBy())
                return (true, "Permission check passed");

            return (false, $"No permission to {_action} on {_permission.MenuName}");
        }
    }
}
