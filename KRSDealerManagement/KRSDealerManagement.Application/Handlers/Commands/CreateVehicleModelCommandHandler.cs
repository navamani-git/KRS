using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Domain.Entities;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    /// <summary>
    /// Handler for creating vehicle model
    /// Automatically logs to AuditLog
    /// </summary>
    public class CreateVehicleModelCommandHandler : IRequestHandler<CreateVehicleModelCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public CreateVehicleModelCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<int> Handle(CreateVehicleModelCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Create entity
                var model = new VehicleModel
                {
                    ModelName = request.ModelName,
                    Description = request.Description,
                    IsActive = true,
                    CreatedBy = request.CreatedBy,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };

                // Add to repository
                var modelId = await _unitOfWork.VehicleModels.AddAsync(model);
                await _unitOfWork.SaveChangesAsync();

                // Log to audit trail
                await _auditService.LogActionAsync(
                    entityType: "VehicleModel",
                    entityId: modelId,
                    action: "Create",
                    userId: request.CreatedBy,
                    userRole: "Admin",
                    newValue: JsonSerializer.Serialize(new { ModelName = request.ModelName, Description = request.Description })
                );

                return modelId;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error creating vehicle model: {ex.Message}", ex);
            }
        }
    }
}
