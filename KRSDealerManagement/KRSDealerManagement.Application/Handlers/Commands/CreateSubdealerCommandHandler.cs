using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Domain.Entities;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    /// <summary>Creates SubDealer org only — add logins from Subdealer Details.</summary>
    public class CreateSubdealerCommandHandler : IRequestHandler<CreateSubdealerCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public CreateSubdealerCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<int> Handle(CreateSubdealerCommand request, CancellationToken cancellationToken)
        {
            var dealership = await _unitOfWork.Dealerships.GetByIdAsync(request.DealershipId);
            if (dealership == null || !dealership.IsActive)
                throw new InvalidOperationException("Dealership location not found or inactive.");

            if (await SubdealerOrgService.IsOrgNameTakenAsync(_unitOfWork, request.DealershipId, request.SubdealerName))
                throw new InvalidOperationException(
                    $"A subdealer named '{request.SubdealerName.Trim()}' already exists at this location.");

            var code = GenerateCode(request.SubdealerName);

            var subDealerId = await _unitOfWork.SubDealers.AddAsync(new SubDealer
            {
                DealershipId = request.DealershipId,
                SubDealerCode = code,
                SubDealerName = request.SubdealerName.Trim(),
                Location = request.Location.Trim(),
                PrimaryPhone = request.PrimaryPhone.Trim(),
                SecondaryPhone = request.SecondaryPhone?.Trim(),
                SalesRepMobile = request.SalesRepMobile?.Trim(),
                ServiceRepMobile = request.ServiceRepMobile?.Trim(),
                Email = request.Email.Trim(),
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                entityType: "SubDealer",
                entityId: subDealerId,
                action: "Create",
                userId: request.CreatedBy,
                userRole: "Staff",
                newValue: JsonSerializer.Serialize(new
                {
                    SubDealerId = subDealerId,
                    request.DealershipId,
                    Dealership = dealership.DealershipCode,
                    Name = request.SubdealerName
                }));

            return subDealerId;
        }

        private static string GenerateCode(string name)
        {
            var code = name.ToLower().Replace(" ", "_").Replace(".", "").Replace(",", "");
            return code.Length > 30 ? code[..30] : code;
        }
    }
}
