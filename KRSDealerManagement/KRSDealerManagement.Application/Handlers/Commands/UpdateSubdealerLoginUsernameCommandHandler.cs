using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class UpdateSubdealerLoginUsernameCommandHandler : IRequestHandler<UpdateSubdealerLoginUsernameCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public UpdateSubdealerLoginUsernameCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(UpdateSubdealerLoginUsernameCommand request, CancellationToken cancellationToken)
        {
            var username = request.Username.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(username))
                throw new InvalidOperationException("Username is required.");

            var user = await _unitOfWork.Users.GetByIdAsync(request.LoginUserId);
            if (user == null || user.UserRole != 2)
                throw new InvalidOperationException("Login user not found.");

            var orgLogins = await SubdealerOrgService.GetLoginsForOrgAsync(_unitOfWork, request.SubDealerId);
            if (!orgLogins.Any(l => l.UserId == request.LoginUserId))
                throw new InvalidOperationException("This login does not belong to the selected subdealer.");

            var duplicate = (await _unitOfWork.Users.GetAllAsync())
                .Any(u => u.UserId != request.LoginUserId
                    && u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (duplicate)
                throw new InvalidOperationException($"Username '{username}' is already taken.");

            var oldUsername = user.Username;
            user.Username = username;
            user.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                entityType: "User",
                entityId: user.UserId,
                action: "UpdateUsername",
                userId: request.UpdatedBy,
                userRole: "Admin",
                oldValue: JsonSerializer.Serialize(new { Username = oldUsername }),
                newValue: JsonSerializer.Serialize(new { Username = username }));

            return true;
        }
    }
}
