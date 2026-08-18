using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class AdminCorrectPaymentCommandHandler : IRequestHandler<AdminCorrectPaymentCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public AdminCorrectPaymentCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(AdminCorrectPaymentCommand request, CancellationToken cancellationToken)
        {
            var payment = await _unitOfWork.Payments.GetByIdAsync(request.PaymentId);
            if (payment == null) return false;

            var paymentTypes = (await _unitOfWork.PaymentTypes.GetAllAsync()).ToList();
            var type = paymentTypes.FirstOrDefault(t => t.PaymentTypeId == request.PaymentTypeId);
            if (type == null) throw new InvalidOperationException("Invalid payment type.");

            var changes = new List<string>();
            var oldStatus = payment.Status;
            var wasApplied = payment.IsApplied;
            var oldCredited = GetCreditedAmount(payment);
            var newCredited = request.Status == 1
                ? (request.ActualReceivedAmount ?? request.Amount)
                : 0;

            if (payment.Amount != request.Amount)
                changes.Add(CorrectionNoteHelper.DescribeChange("Requested Amount", $"₹{payment.Amount:N2}", $"₹{request.Amount:N2}"));

            var oldReceived = payment.ActualReceivedAmount;
            if (oldReceived != request.ActualReceivedAmount)
                changes.Add(CorrectionNoteHelper.DescribeChange("Actual Received Amount",
                    oldReceived.HasValue ? $"₹{oldReceived.Value:N2}" : "(none)",
                    request.ActualReceivedAmount.HasValue ? $"₹{request.ActualReceivedAmount.Value:N2}" : "(none)"));

            var oldReceivedDate = payment.ActualReceivedDate?.ToString("yyyy-MM-dd");
            var newReceivedDate = request.ActualReceivedDate?.ToString("yyyy-MM-dd");
            if (oldReceivedDate != newReceivedDate)
                changes.Add(CorrectionNoteHelper.DescribeChange("Actual Received Date", oldReceivedDate, newReceivedDate));

            if (payment.PaymentTypeId != request.PaymentTypeId)
                changes.Add(CorrectionNoteHelper.DescribeChange("Payment Type", payment.PaymentType, type.TypeName));
            if (payment.PaymentDate.Date != request.PaymentDate.Date)
                changes.Add(CorrectionNoteHelper.DescribeChange("Payment Date", payment.PaymentDate.ToString("yyyy-MM-dd"), request.PaymentDate.ToString("yyyy-MM-dd")));
            if (payment.Status != request.Status)
                changes.Add(CorrectionNoteHelper.DescribeChange("Status", payment.GetStatusDisplay(), StatusLabel(request.Status)));
            if (!string.Equals(payment.CustomerName, request.CustomerName, StringComparison.OrdinalIgnoreCase))
                changes.Add(CorrectionNoteHelper.DescribeChange("Customer", payment.CustomerName, request.CustomerName));
            if (payment.FinanceNameId != request.FinanceNameId)
                changes.Add(CorrectionNoteHelper.DescribeChange("Finance", payment.FinanceNameId, request.FinanceNameId));
            if (!string.Equals(payment.VinNumber, request.VinNumber, StringComparison.OrdinalIgnoreCase))
                changes.Add(CorrectionNoteHelper.DescribeChange("VIN", payment.VinNumber, request.VinNumber));
            if (!string.Equals(payment.SubdealerRemarks, request.SubdealerRemarks, StringComparison.Ordinal))
                changes.Add(CorrectionNoteHelper.DescribeChange("Subdealer Remarks", payment.SubdealerRemarks, request.SubdealerRemarks));

            payment.Amount = request.Amount;
            payment.ActualReceivedAmount = request.Status == 1 ? request.ActualReceivedAmount ?? request.Amount : null;
            payment.ActualReceivedDate = request.Status == 1 ? request.ActualReceivedDate?.Date : null;
            payment.PaymentTypeId = request.PaymentTypeId;
            payment.PaymentType = type.TypeName;
            payment.PaymentDate = request.PaymentDate.Date;
            payment.Status = request.Status;
            payment.CustomerName = string.IsNullOrWhiteSpace(request.CustomerName)
                ? null
                : request.CustomerName.Trim().ToUpperInvariant();
            payment.FinanceNameId = request.FinanceNameId;
            payment.VinNumber = string.IsNullOrWhiteSpace(request.VinNumber)
                ? null
                : request.VinNumber.Trim().ToUpperInvariant();
            payment.SubdealerRemarks = request.SubdealerRemarks?.Trim();
            payment.ModifiedDate = DateTime.UtcNow;

            var noteEntry = CorrectionNoteHelper.FormatEntry(request.CorrectedByName, request.CorrectionReason, changes);
            payment.DealerRemarks = CorrectionNoteHelper.Append(payment.DealerRemarks, noteEntry);

            await _unitOfWork.Payments.UpdateAsync(payment);

            if (wasApplied && request.Status == 1 && newCredited != oldCredited)
            {
                await AdjustBalanceAsync(payment, newCredited - oldCredited, request.CorrectedBy, noteEntry);
            }

            if (wasApplied && oldStatus == 1 && request.Status != 1)
            {
                await AdjustBalanceAsync(payment, -oldCredited, request.CorrectedBy,
                    $"Reversed credited amount due to status change to {StatusLabel(request.Status)}.");
                payment.IsApplied = false;
                payment.ActualReceivedAmount = null;
                payment.ActualReceivedDate = null;
                await _unitOfWork.Payments.UpdateAsync(payment);
            }
            else if (!wasApplied && request.Status == 1 && oldStatus != 1)
            {
                await AdjustBalanceAsync(payment, newCredited, request.CorrectedBy,
                    "Amount credited due to admin status correction to Approved.");
                payment.IsApplied = true;
                await _unitOfWork.Payments.UpdateAsync(payment);
            }

            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                entityType: "Payment",
                entityId: payment.PaymentId,
                action: "AdminCorrection",
                userId: request.CorrectedBy,
                userRole: "Admin",
                newValue: JsonSerializer.Serialize(new
                {
                    request.CorrectionReason,
                    changes
                }),
                remarks: noteEntry);

            return true;
        }

        private static decimal GetCreditedAmount(Domain.Entities.Payment payment)
            => payment.ActualReceivedAmount ?? payment.Amount;

        private static string StatusLabel(int status) => status switch
        {
            0 => "Pending",
            1 => "Approved",
            2 => "Rejected",
            _ => status.ToString()
        };

        private async Task AdjustBalanceAsync(Domain.Entities.Payment payment, decimal delta, int correctedBy, string note)
        {
            if (delta == 0) return;

            var balance = (await _unitOfWork.AccountBalances.GetAllAsync())
                .FirstOrDefault(b => b.SubdealerAccountId == payment.AccountId)
                ?? (await _unitOfWork.AccountBalances.GetAllAsync())
                    .FirstOrDefault(b => b.SubdealerId == payment.SubdealerId);
            if (balance == null) return;

            balance.CurrentBalance += delta;
            balance.AvailableBalance = balance.CurrentBalance - balance.ReservedAmount;
            balance.LastTransactionDate = DateTime.UtcNow;
            balance.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.AccountBalances.UpdateAsync(balance);

            await _auditService.LogTransactionAsync(
                accountId: payment.AccountId,
                transactionType: delta >= 0 ? 2 : 1,
                amount: Math.Abs(delta),
                balanceAfter: balance.CurrentBalance,
                reason: $"Admin payment correction #{payment.PaymentId}",
                referenceType: "Payment",
                referenceId: payment.PaymentId,
                remarks: note,
                initiatedBy: correctedBy);
        }
    }
}
