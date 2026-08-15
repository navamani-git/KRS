using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class ConfigureAccountPermissionsCommandHandler : IRequestHandler<ConfigureAccountPermissionsCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public ConfigureAccountPermissionsCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(ConfigureAccountPermissionsCommand request, CancellationToken cancellationToken)
        {
            var existing = (await _unitOfWork.AccountPermissions.GetAllAsync())
                .Where(p => p.AccountId == request.AccountId)
                .ToList();

            foreach (var setting in request.Permissions)
            {
                var row = existing.FirstOrDefault(p =>
                    p.MenuKey.Equals(setting.MenuKey, StringComparison.OrdinalIgnoreCase));

                if (row == null)
                {
                    await _unitOfWork.AccountPermissions.AddAsync(new AccountPermission
                    {
                        AccountId = request.AccountId,
                        MenuKey = setting.MenuKey,
                        MenuName = setting.MenuName,
                        IsAccessible = setting.IsAccessible,
                        CanCreate = setting.CanCreate,
                        CanEdit = setting.CanEdit,
                        CanDelete = setting.CanDelete,
                        CanApprove = setting.CanApprove,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow
                    });
                }
                else
                {
                    row.MenuName = setting.MenuName;
                    row.IsAccessible = setting.IsAccessible;
                    row.CanCreate = setting.CanCreate;
                    row.CanEdit = setting.CanEdit;
                    row.CanDelete = setting.CanDelete;
                    row.CanApprove = setting.CanApprove;
                    row.ModifiedDate = DateTime.UtcNow;
                    await _unitOfWork.AccountPermissions.UpdateAsync(row);
                }
            }

            var postedKeys = new HashSet<string>(request.Permissions.Select(p => p.MenuKey), StringComparer.OrdinalIgnoreCase);
            foreach (var menu in MenuKeys.GetSubdealerConfigurableMenus())
            {
                if (postedKeys.Contains(menu.Key)) continue;
                var row = existing.FirstOrDefault(p => p.MenuKey.Equals(menu.Key, StringComparison.OrdinalIgnoreCase));
                if (row != null)
                {
                    row.IsAccessible = false;
                    row.CanCreate = false;
                    row.CanEdit = false;
                    row.ModifiedDate = DateTime.UtcNow;
                    await _unitOfWork.AccountPermissions.UpdateAsync(row);
                }
                else
                {
                    await _unitOfWork.AccountPermissions.AddAsync(new AccountPermission
                    {
                        AccountId = request.AccountId,
                        MenuKey = menu.Key,
                        MenuName = menu.Name,
                        IsAccessible = false,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                entityType: "AccountPermission",
                entityId: request.AccountId,
                action: "Configure",
                userId: request.ConfiguredBy,
                userRole: "Admin",
                newValue: JsonSerializer.Serialize(request.Permissions),
                remarks: request.Remarks
            );

            return true;
        }
    }
}
