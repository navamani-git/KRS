using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    /// <summary>
    /// Handler for updating vehicle model
    /// Automatically logs changes to AuditLog
    /// </summary>
    public class UpdateVehicleModelCommandHandler : IRequestHandler<UpdateVehicleModelCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public UpdateVehicleModelCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(UpdateVehicleModelCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Get existing model
                var model = await _unitOfWork.VehicleModels.GetByIdAsync(request.ModelId);
                
                if (model == null)
                    return false;

                // Store old values for audit
                var oldValues = JsonSerializer.Serialize(new
                {
                    ModelName = model.ModelName,
                    Description = model.Description,
                    IsActive = model.IsActive
                });

                // Update model
                model.ModelName = request.ModelName;
                model.Description = request.Description;
                model.IsActive = request.IsActive;
                model.ModifiedBy = request.ModifiedBy;
                model.ModifiedDate = DateTime.UtcNow;

                // Save changes
                var result = await _unitOfWork.VehicleModels.UpdateAsync(model);
                await _unitOfWork.SaveChangesAsync();

                // Log to audit trail
                await _auditService.LogActionAsync(
                    entityType: "VehicleModel",
                    entityId: model.ModelId,
                    action: "Update",
                    userId: request.ModifiedBy,
                    userRole: "Admin",
                    oldValue: oldValues,
                    newValue: JsonSerializer.Serialize(new
                    {
                        ModelName = request.ModelName,
                        Description = request.Description,
                        IsActive = request.IsActive,
                        Remarks = request.Remarks
                    })
                );

                return result;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error updating vehicle model: {ex.Message}", ex);
            }
        }
    }
}
