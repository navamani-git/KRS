namespace KRSDealerManagement.Shared.Enums
{
    /// <summary>
    /// Types of account transactions for audit trail
    /// </summary>
    public enum TransactionTypeEnum
    {
        /// <summary>
        /// Initial balance set by admin when account created
        /// </summary>
        InitialBalance = 1,

        /// <summary>
        /// Amount reserved for pending purchase order
        /// </summary>
        PurchaseOrderCreated = 2,

        /// <summary>
        /// Purchase order approved - amount debited
        /// </summary>
        PurchaseOrderApproved = 3,

        /// <summary>
        /// Purchase order rejected - reserved amount released
        /// </summary>
        PurchaseOrderRejected = 4,

        /// <summary>
        /// Vehicle price increased - additional amount debited
        /// </summary>
        PriceIncreaseDebit = 5,

        /// <summary>
        /// Vehicle price decreased - difference credited back
        /// </summary>
        PriceDecreaseCredit = 6,

        /// <summary>
        /// Commission approved - amount credited to account
        /// </summary>
        CommissionApproved = 7,

        /// <summary>
        /// Commission rejected - no balance change but recorded
        /// </summary>
        CommissionRejected = 8,

        /// <summary>
        /// Manual adjustment by admin
        /// </summary>
        ManualAdjustment = 9
    }

    /// <summary>
    /// Extension methods for TransactionTypeEnum
    /// </summary>
    public static class TransactionTypeEnumExtensions
    {
        public static string GetDisplayName(this TransactionTypeEnum type)
        {
            return type switch
            {
                TransactionTypeEnum.InitialBalance => "Initial Balance",
                TransactionTypeEnum.PurchaseOrderCreated => "Purchase Order Created",
                TransactionTypeEnum.PurchaseOrderApproved => "Purchase Order Approved",
                TransactionTypeEnum.PurchaseOrderRejected => "Purchase Order Rejected",
                TransactionTypeEnum.PriceIncreaseDebit => "Price Increase",
                TransactionTypeEnum.PriceDecreaseCredit => "Price Decrease",
                TransactionTypeEnum.CommissionApproved => "Commission Approved",
                TransactionTypeEnum.CommissionRejected => "Commission Rejected",
                TransactionTypeEnum.ManualAdjustment => "Manual Adjustment",
                _ => "Unknown"
            };
        }

        public static bool IsDebit(this TransactionTypeEnum type)
        {
            return type switch
            {
                TransactionTypeEnum.PurchaseOrderCreated => true,
                TransactionTypeEnum.PurchaseOrderApproved => true,
                TransactionTypeEnum.PriceIncreaseDebit => true,
                _ => false
            };
        }

        public static bool IsCredit(this TransactionTypeEnum type)
        {
            return type switch
            {
                TransactionTypeEnum.PurchaseOrderRejected => true,
                TransactionTypeEnum.PriceDecreaseCredit => true,
                TransactionTypeEnum.CommissionApproved => true,
                TransactionTypeEnum.InitialBalance => true,
                _ => false
            };
        }
    }
}
