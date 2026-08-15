using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Domain.Entities;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class CreateVehicleColorCommandHandler : IRequestHandler<CreateVehicleColorCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public CreateVehicleColorCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<int> Handle(CreateVehicleColorCommand request, CancellationToken cancellationToken)
        {
            var color = new VehicleColor
            {
                ColorName = request.ColorName,
                HexCode = request.HexCode,
                IsActive = true,
                CreatedBy = request.CreatedBy,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            var colorId = await _unitOfWork.VehicleColors.AddAsync(color);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                entityType: "VehicleColor",
                entityId: colorId,
                action: "Create",
                userId: request.CreatedBy,
                userRole: "Admin",
                newValue: JsonSerializer.Serialize(new { ColorName = request.ColorName, HexCode = request.HexCode })
            );

            return colorId;
        }
    }

    public class UpdateVehicleColorCommandHandler : IRequestHandler<UpdateVehicleColorCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public UpdateVehicleColorCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(UpdateVehicleColorCommand request, CancellationToken cancellationToken)
        {
            var color = await _unitOfWork.VehicleColors.GetByIdAsync(request.ColorId);
            if (color == null) return false;

            var oldValues = JsonSerializer.Serialize(new { color.ColorName, color.HexCode, color.IsActive });

            color.ColorName = request.ColorName;
            color.HexCode = request.HexCode;
            color.IsActive = request.IsActive;
            color.ModifiedBy = request.ModifiedBy;
            color.ModifiedDate = DateTime.UtcNow;

            var result = await _unitOfWork.VehicleColors.UpdateAsync(color);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                entityType: "VehicleColor",
                entityId: color.ColorId,
                action: "Update",
                userId: request.ModifiedBy,
                userRole: "Admin",
                newValue: JsonSerializer.Serialize(new { color.ColorName, color.HexCode, color.IsActive }),
                oldValue: oldValues
            );

            return result;
        }
    }
}
