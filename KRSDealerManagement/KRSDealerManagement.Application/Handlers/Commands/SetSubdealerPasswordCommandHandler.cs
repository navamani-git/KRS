using MediatR;
using KRSDealerManagement.Domain.Repositories;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>
    /// Admin sets / resets a subdealer login password.
    /// </summary>
    public class SetSubdealerPasswordCommand : IRequest<bool>
    {
        public int SubdealerId { get; set; }
        public required string Password { get; set; }
        public int UpdatedBy { get; set; }
    }
}

namespace KRSDealerManagement.Application.Handlers.Commands
{
    using KRSDealerManagement.Application.Commands;
    using KRSDealerManagement.Application.Services;
    using System.Text.Json;

    public class SetSubdealerPasswordCommandHandler : IRequestHandler<SetSubdealerPasswordCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public SetSubdealerPasswordCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(SetSubdealerPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(request.SubdealerId);
            if (user == null || user.UserRole != 2)
                throw new InvalidOperationException("Subdealer not found.");

            user.PasswordHash = request.Password.Trim();
            user.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                entityType: "User",
                entityId: user.UserId,
                action: "SetPassword",
                userId: request.UpdatedBy,
                userRole: "Admin",
                newValue: JsonSerializer.Serialize(new { Username = user.Username })
            );

            return true;
        }
    }
}
