using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Domain.Entities;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class CreateCommissionRateCommandHandler : IRequestHandler<CreateCommissionRateCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public CreateCommissionRateCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<int> Handle(CreateCommissionRateCommand request, CancellationToken cancellationToken)
        {
            var rate = new CommissionRate
            {
                ModelId = request.ModelId,
                CommissionAmount = request.CommissionAmount,
                StartMonth = request.StartMonth,
                StartYear = request.StartYear,
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                Notes = request.Notes,
                CreatedBy = request.CreatedBy,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            var rateId = await _unitOfWork.CommissionRates.AddAsync(rate);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                entityType: "CommissionRate",
                entityId: rateId,
                action: "Create",
                userId: request.CreatedBy,
                userRole: "Admin",
                newValue: JsonSerializer.Serialize(new
                {
                    ModelId = request.ModelId,
                    Amount = request.CommissionAmount,
                    Start = $"{request.StartYear}-{request.StartMonth:D2}",
                    Expiry = request.ExpiryYear.HasValue ? $"{request.ExpiryYear}-{request.ExpiryMonth:D2}" : "Ongoing"
                })
            );

            return rateId;
        }
    }
}
