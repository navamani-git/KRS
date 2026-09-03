using MediatR;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetAccountPermissionsQueryHandler : IRequestHandler<GetAccountPermissionsQuery, IEnumerable<AccountPermissionDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAccountPermissionsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<AccountPermissionDto>> Handle(GetAccountPermissionsQuery request, CancellationToken cancellationToken)
        {
            var rows = (await _unitOfWork.AccountPermissions.GetAllAsync())
                .Where(p => p.AccountId == request.AccountId);

            if (request.IsAccessibleOnly == true)
                rows = rows.Where(p => p.IsAccessible);

            var list = rows
                .GroupBy(p => p.MenuKey, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var p = g.OrderByDescending(x => x.ModifiedDate).First();
                    return new AccountPermissionDto
                    {
                        PermissionId = p.PermissionId,
                        AccountId = p.AccountId,
                        MenuKey = p.MenuKey,
                        MenuName = p.MenuName,
                        IsAccessible = g.Any(x => x.IsAccessible),
                        CanCreate = g.Any(x => x.CanCreate),
                        CanEdit = g.Any(x => x.CanEdit),
                        CanDelete = g.Any(x => x.CanDelete),
                        CanApprove = g.Any(x => x.CanApprove),
                        CreatedDate = p.CreatedDate,
                        ModifiedDate = p.ModifiedDate
                    };
                })
                .ToList();

            // No rows yet → defaults (all accessible) so existing dealers keep working
            if (!list.Any())
            {
                list = MenuKeys.GetSubdealerConfigurableMenus().Select(m => new AccountPermissionDto
                {
                    PermissionId = 0,
                    AccountId = request.AccountId,
                    MenuKey = m.Key,
                    MenuName = m.Name,
                    IsAccessible = m.DefaultAccessible,
                    CanCreate = m.DefaultAccessible,
                    CanEdit = m.DefaultAccessible,
                    CanDelete = false,
                    CanApprove = false,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                }).ToList();
            }

            return list.OrderBy(p => p.MenuName).ToList();
        }
    }
}
