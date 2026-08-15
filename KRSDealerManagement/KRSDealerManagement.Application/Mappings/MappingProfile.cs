using AutoMapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Mappings
{
    /// <summary>
    /// AutoMapper configuration for mapping entities to DTOs
    /// Automatically registered via DependencyInjection.cs
    /// </summary>
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User Entity -> DTO
            CreateMap<User, UserDto>();

            // SubdealerAccount Entity -> DTO
            CreateMap<SubdealerAccount, SubdealerAccountDto>();

            // AccountPermission Entity -> DTO
            CreateMap<AccountPermission, AccountPermissionDto>();

            // AccountBalance Entity -> DTO
            CreateMap<AccountBalance, AccountBalanceDto>();

            // VehicleModel Entity -> DTO
            CreateMap<VehicleModel, VehicleModelDto>();

            // VehicleColor Entity -> DTO
            CreateMap<VehicleColor, VehicleColorDto>();

            // Vehicle Entity -> DTO
            CreateMap<Vehicle, VehicleDto>();

            // VehiclePriceHistory Entity -> DTO
            CreateMap<VehiclePriceHistory, VehiclePriceHistoryDto>();

            // PurchaseOrder Entity -> DTO
            CreateMap<PurchaseOrder, PurchaseOrderDto>();

            // Commission Entity -> DTO
            CreateMap<Commission, CommissionDto>();

            // CommissionRate Entity -> DTO
            CreateMap<CommissionRate, CommissionRateDto>();

            // ReturnRequest Entity -> DTO
            CreateMap<ReturnRequest, ReturnRequestDto>();

            // Payment Entity -> DTO
            CreateMap<Payment, PaymentDto>();

            // AccountTransaction Entity -> DTO
            CreateMap<AccountTransaction, AccountTransactionDto>();

            // AuditLog Entity -> DTO
            CreateMap<AuditLog, AuditLogDto>();
        }
    }
}
