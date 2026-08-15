using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Domain.DomainServices
{
    /// <summary>
    /// Domain service for validating account balance operations
    /// </summary>
    public interface IBalanceValidationService
    {
        /// <summary>
        /// Validate if account has sufficient balance for transaction
        /// </summary>
        bool HasSufficientBalance(AccountBalance balance, decimal amount);

        /// <summary>
        /// Validate if amount can be reserved for pending order
        /// </summary>
        bool CanReserveAmount(AccountBalance balance, decimal amount);

        /// <summary>
        /// Validate if amount can be released from reserved
        /// </summary>
        bool CanReleaseReservedAmount(AccountBalance balance, decimal amount);

        /// <summary>
        /// Get validation error message for insufficient balance
        /// </summary>
        string GetInsufficientBalanceMessage(AccountBalance balance, decimal requiredAmount);
    }
}
