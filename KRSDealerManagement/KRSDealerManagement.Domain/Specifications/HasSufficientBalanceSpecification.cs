using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Shared.Exceptions;

namespace KRSDealerManagement.Domain.Specifications
{
    /// <summary>
    /// Business rule: Account must have sufficient available balance for transaction
    /// Used to validate purchase orders before approval
    /// </summary>
    public class HasSufficientBalanceSpecification
    {
        private readonly AccountBalance _balance;
        private readonly decimal _requiredAmount;

        public HasSufficientBalanceSpecification(AccountBalance balance, decimal requiredAmount)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _requiredAmount = requiredAmount;
        }

        /// <summary>
        /// Check if specification is satisfied
        /// </summary>
        public bool IsSatisfiedBy()
        {
            return _balance.HasSufficientBalance(_requiredAmount);
        }

        /// <summary>
        /// Validate and throw exception if not satisfied
        /// </summary>
        public void Validate()
        {
            if (!IsSatisfiedBy())
            {
                throw new DomainException(
                    $"Insufficient balance. Required: ₹{_requiredAmount:N2}, Available: ₹{_balance.AvailableBalance:N2}"
                );
            }
        }

        /// <summary>
        /// Get validation result with message
        /// </summary>
        public (bool IsValid, string Message) GetValidationResult()
        {
            if (IsSatisfiedBy())
                return (true, "Balance check passed");

            return (false, $"Insufficient balance. Required: ₹{_requiredAmount:N2}, Available: ₹{_balance.AvailableBalance:N2}");
        }
    }
}
