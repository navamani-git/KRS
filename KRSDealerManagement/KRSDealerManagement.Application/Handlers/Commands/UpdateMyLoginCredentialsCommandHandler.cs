using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class UpdateMyLoginCredentialsCommandHandler : IRequestHandler<UpdateMyLoginCredentialsCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public UpdateMyLoginCredentialsCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(UpdateMyLoginCredentialsCommand request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId)
                ?? throw new InvalidOperationException("User account not found.");

            if (!user.IsActive)
                throw new InvalidOperationException("Your account is inactive. Contact administrator.");

            if (!LoginCredentialHelper.VerifyPassword(user.PasswordHash, request.CurrentPassword))
                throw new InvalidOperationException("Current password is incorrect.");

            var username = LoginCredentialHelper.NormalizeUsername(request.Username);
            var usernameChanged = !string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase);
            var passwordChanged = !string.IsNullOrWhiteSpace(request.NewPassword);

            if (!usernameChanged && !passwordChanged)
                throw new InvalidOperationException("No changes to save.");

            if (usernameChanged)
            {
                var duplicate = (await _unitOfWork.Users.GetAllAsync())
                    .Any(u => u.UserId != user.UserId
                        && u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
                if (duplicate)
                    throw new InvalidOperationException($"Username '{username}' is already taken.");
            }

            var oldUsername = user.Username;
            if (usernameChanged)
                user.Username = username;
            if (passwordChanged)
                user.PasswordHash = request.NewPassword!.Trim();

            user.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                entityType: "User",
                entityId: user.UserId,
                action: "UpdateOwnCredentials",
                userId: user.UserId,
                userRole: "Self",
                oldValue: JsonSerializer.Serialize(new { Username = oldUsername }),
                newValue: JsonSerializer.Serialize(new
                {
                    Username = user.Username,
                    PasswordChanged = passwordChanged
                }));

            return true;
        }
    }
}
