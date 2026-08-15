using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Enums;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class RejectCommissionCommandHandler : IRequestHandler<RejectCommissionCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public RejectCommissionCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(RejectCommissionCommand request, CancellationToken cancellationToken)
        {
            var commission = await _unitOfWork.Commissions.GetByIdAsync(request.CommissionId);
            if (commission == null || !commission.CanBeApproved())
                return false;

            commission.Reject(request.RejectedBy);
            var note = $"[Rejected] {request.Remarks.Trim()}";
            commission.Notes = string.IsNullOrWhiteSpace(commission.Notes)
                ? note
                : $"{commission.Notes} {note}";

            await _unitOfWork.Commissions.UpdateAsync(commission);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                entityType: "Commission",
                entityId: commission.CommissionId,
                action: "Reject",
                userId: request.RejectedBy,
                userRole: "Admin",
                newValue: JsonSerializer.Serialize(new
                {
                    commission.CommissionId,
                    request.Remarks
                }));

            return true;
        }
    }
}
