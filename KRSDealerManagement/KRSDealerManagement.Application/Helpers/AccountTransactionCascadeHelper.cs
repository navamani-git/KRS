using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Enums;
using KRSDealerManagement.Shared.Helpers;

namespace KRSDealerManagement.Application.Helpers
{
    public static class AccountTransactionCascadeHelper
    {
        public static async Task<object?> LoadLinkedSnapshotAsync(IUnitOfWork unitOfWork, AccountTransaction transaction)
        {
            if (!transaction.ReferenceId.HasValue || string.IsNullOrWhiteSpace(transaction.ReferenceType))
                return null;

            return transaction.ReferenceType.Trim() switch
            {
                "Payment" => await unitOfWork.Payments.GetByIdAsync(transaction.ReferenceId.Value),
                "Commission" => await unitOfWork.Commissions.GetByIdAsync(transaction.ReferenceId.Value),
                "PurchaseOrder" or "Order" => await unitOfWork.PurchaseOrders.GetByIdAsync(transaction.ReferenceId.Value),
                "ReturnRequest" => await unitOfWork.ReturnRequests.GetByIdAsync(transaction.ReferenceId.Value),
                "Vehicle" => await unitOfWork.Vehicles.GetByIdAsync(transaction.ReferenceId.Value),
                _ => null
            };
        }

        public static async Task ApplyEditCascadeAsync(
            IUnitOfWork unitOfWork,
            AccountTransaction transaction,
            decimal? requestedAmount,
            decimal? approvedPaymentAmount,
            DateTime? paymentSubmittedDate,
            DateTime? paymentApprovedDate,
            DateTime? paymentReceivedDate,
            string? customerName,
            int? paymentTypeId,
            int? financeNameId,
            string? vinNumber,
            decimal? commissionAmount)
        {
            if (!transaction.ReferenceId.HasValue || string.IsNullOrWhiteSpace(transaction.ReferenceType))
                return;

            switch (transaction.ReferenceType.Trim())
            {
                case "Payment":
                    await UpdatePaymentAsync(
                        unitOfWork, transaction,
                        requestedAmount, approvedPaymentAmount,
                        paymentSubmittedDate, paymentApprovedDate, paymentReceivedDate,
                        customerName, paymentTypeId, financeNameId, vinNumber);
                    break;
                case "Commission":
                    if (commissionAmount.HasValue)
                    {
                        var commission = await unitOfWork.Commissions.GetByIdAsync(transaction.ReferenceId.Value);
                        if (commission != null)
                        {
                            commission.CommissionAmount = commissionAmount.Value;
                            commission.ApprovedAmount = commissionAmount.Value;
                            commission.ModifiedDate = DateTime.UtcNow;
                            await unitOfWork.Commissions.UpdateAsync(commission);
                        }
                    }
                    break;
                case "PurchaseOrder":
                case "Order":
                    var order = await unitOfWork.PurchaseOrders.GetByIdAsync(transaction.ReferenceId.Value);
                    if (order != null)
                    {
                        order.TotalAmount = transaction.Amount;
                        order.ModifiedDate = DateTime.UtcNow;
                        await unitOfWork.PurchaseOrders.UpdateAsync(order);
                    }
                    break;
            }
        }

        public static async Task ApplyDeleteCascadeAsync(IUnitOfWork unitOfWork, AccountTransaction transaction)
        {
            if (!transaction.ReferenceId.HasValue || string.IsNullOrWhiteSpace(transaction.ReferenceType))
                return;

            switch (transaction.ReferenceType.Trim())
            {
                case "Payment":
                    var payment = await unitOfWork.Payments.GetByIdAsync(transaction.ReferenceId.Value);
                    if (payment != null)
                    {
                        payment.IsApplied = false;
                        payment.TransactionId = null;
                        payment.ModifiedDate = DateTime.UtcNow;
                        await unitOfWork.Payments.UpdateAsync(payment);
                    }
                    break;
                case "Commission":
                    var commission = await unitOfWork.Commissions.GetByIdAsync(transaction.ReferenceId.Value);
                    if (commission != null && commission.Status == (int)CommissionStatusEnum.Paid)
                    {
                        commission.Status = (int)CommissionStatusEnum.Approved;
                        commission.PaidDate = null;
                        commission.ModifiedDate = DateTime.UtcNow;
                        await unitOfWork.Commissions.UpdateAsync(commission);
                    }
                    break;
            }
        }

        private static async Task UpdatePaymentAsync(
            IUnitOfWork unitOfWork,
            AccountTransaction transaction,
            decimal? requestedAmount,
            decimal? approvedPaymentAmount,
            DateTime? paymentSubmittedDate,
            DateTime? paymentApprovedDate,
            DateTime? paymentReceivedDate,
            string? customerName,
            int? paymentTypeId,
            int? financeNameId,
            string? vinNumber)
        {
            var payment = await unitOfWork.Payments.GetByIdAsync(transaction.ReferenceId!.Value);
            if (payment == null) return;

            if (requestedAmount.HasValue)
                payment.Amount = requestedAmount.Value;
            if (approvedPaymentAmount.HasValue)
                payment.ActualReceivedAmount = approvedPaymentAmount.Value;
            else if (AccountTransactionTypeHelper.IsCredit(transaction.TransactionType))
                payment.ActualReceivedAmount = transaction.Amount;

            if (paymentSubmittedDate.HasValue)
                payment.CreatedDate = paymentSubmittedDate.Value;
            if (paymentApprovedDate.HasValue)
                payment.ProcessedDate = paymentApprovedDate.Value;
            if (paymentReceivedDate.HasValue)
                payment.ActualReceivedDate = paymentReceivedDate.Value.Date;

            if (!string.IsNullOrWhiteSpace(customerName))
                payment.CustomerName = customerName.Trim().ToUpperInvariant();
            if (paymentTypeId.HasValue)
            {
                var types = await unitOfWork.PaymentTypes.GetAllAsync();
                var type = types.FirstOrDefault(t => t.PaymentTypeId == paymentTypeId.Value);
                if (type != null)
                {
                    payment.PaymentTypeId = type.PaymentTypeId;
                    payment.PaymentType = type.TypeName;
                }
            }
            if (financeNameId.HasValue)
                payment.FinanceNameId = financeNameId;
            if (!string.IsNullOrWhiteSpace(vinNumber))
                payment.VinNumber = vinNumber.Trim().ToUpperInvariant();

            payment.IsApplied = true;
            payment.TransactionId = transaction.TransactionId;
            payment.ModifiedDate = DateTime.UtcNow;
            await unitOfWork.Payments.UpdateAsync(payment);
        }
    }
}
