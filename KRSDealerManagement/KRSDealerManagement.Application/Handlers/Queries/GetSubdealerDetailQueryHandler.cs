using MediatR;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetSubdealerDetailQueryHandler : IRequestHandler<GetSubdealerDetailQuery, SubdealerDetailDto?>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetSubdealerDetailQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<SubdealerDetailDto?> Handle(GetSubdealerDetailQuery request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
            if (user == null || user.UserRole != 2) return null;

            var assignment = (await _unitOfWork.UserOrgRoles.GetAllAsync())
                .Where(a => a.UserId == request.UserId)
                .OrderByDescending(a => a.IsActive)
                .ThenByDescending(a => a.IsPrimary)
                .FirstOrDefault();
            if (assignment == null) return null;

            if (request.DealershipId.HasValue && assignment.DealershipId != request.DealershipId)
                return null;

            var org = assignment.SubDealerId.HasValue
                ? await _unitOfWork.SubDealers.GetByIdAsync(assignment.SubDealerId.Value)
                : null;

            var dealership = assignment.DealershipId.HasValue
                ? await _unitOfWork.Dealerships.GetByIdAsync(assignment.DealershipId.Value)
                : null;

            return new SubdealerDetailDto
            {
                UserId = user.UserId,
                SubDealerId = org?.SubDealerId,
                DealershipId = assignment.DealershipId ?? org?.DealershipId ?? 0,
                DealershipName = dealership?.DealershipName,
                Username = user.Username,
                PasswordHash = user.PasswordHash,
                SubdealerName = org?.SubDealerName ?? user.FirstName,
                Location = org?.Location ?? user.LastName ?? "",
                Email = org?.Email ?? user.Email,
                PrimaryPhone = org?.PrimaryPhone ?? user.PhoneNumber ?? "",
                SecondaryPhone = org?.SecondaryPhone,
                SalesRepMobile = org?.SalesRepMobile,
                ServiceRepMobile = org?.ServiceRepMobile,
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate
            };
        }
    }
}
