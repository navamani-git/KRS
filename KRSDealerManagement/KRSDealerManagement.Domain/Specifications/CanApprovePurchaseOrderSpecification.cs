using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Shared.Exceptions;

namespace KRSDealerManagement.Domain.Specifications
{
    /// <summary>
    /// Business rule: Purchase order must be in pending state and account balance sufficient to approve
    /// </summary>
    public class CanApprovePurchaseOrderSpecification
    {
        private readonly PurchaseOrder _order;
        private readonly AccountBalance _balance;

        public CanApprovePurchaseOrderSpecification(PurchaseOrder order, AccountBalance balance)
        {
            _order = order ?? throw new ArgumentNullException(nameof(order));
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
        }

        /// <summary>
        /// Check if specification is satisfied
        /// </summary>
        public bool IsSatisfiedBy()
        {
            // Order must be approvable
            if (!_order.CanBeApproved())
                return false;

            // Must have sufficient balance for order amount
            if (!_balance.HasSufficientBalance(_order.TotalAmount))
                return false;

            return true;
        }

        /// <summary>
        /// Validate and throw exception if not satisfied
        /// </summary>
        public void Validate()
        {
            if (!_order.CanBeApproved())
            {
                throw new DomainException(
                    $"Cannot approve order in {_order.GetStatusDisplay()} status"
                );
            }

            if (!_balance.HasSufficientBalance(_order.TotalAmount))
            {
                throw new DomainException(
                    $"Insufficient balance to approve order. Required: ₹{_order.TotalAmount:N2}, Available: ₹{_balance.AvailableBalance:N2}"
                );
            }
        }

        /// <summary>
        /// Get validation result with message
        /// </summary>
        public (bool IsValid, string Message) GetValidationResult()
        {
            if (!_order.CanBeApproved())
                return (false, $"Cannot approve order in {_order.GetStatusDisplay()} status");

            if (!_balance.HasSufficientBalance(_order.TotalAmount))
                return (false, $"Insufficient balance. Required: ₹{_order.TotalAmount:N2}, Available: ₹{_balance.AvailableBalance:N2}");

            return (true, "Order can be approved");
        }
    }
}
