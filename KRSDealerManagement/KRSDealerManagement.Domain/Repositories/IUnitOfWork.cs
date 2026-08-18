using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Domain.Repositories
{
    /// <summary>
    /// Unit of Work pattern interface for managing database transactions
    /// Coordinates multiple repositories and ensures data consistency
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Repository for User entities
        /// </summary>
        IRepository<User> Users { get; }

        /// <summary>
        /// Repository for SubdealerAccount entities
        /// </summary>
        IRepository<SubdealerAccount> SubdealerAccounts { get; }

        /// <summary>
        /// Repository for AccountPermission entities
        /// </summary>
        IRepository<AccountPermission> AccountPermissions { get; }

        /// <summary>
        /// Repository for AccountBalance entities
        /// </summary>
        IRepository<AccountBalance> AccountBalances { get; }

        /// <summary>
        /// Repository for Vehicle entities
        /// </summary>
        IRepository<Vehicle> Vehicles { get; }

        /// <summary>
        /// Repository for VehicleModel entities
        /// </summary>
        IRepository<VehicleModel> VehicleModels { get; }

        /// <summary>
        /// Repository for VehicleColor entities
        /// </summary>
        IRepository<VehicleColor> VehicleColors { get; }

        /// <summary>
        /// Repository for model-to-color mappings
        /// </summary>
        IVehicleModelColorRepository VehicleModelColors { get; }

        /// <summary>
        /// Repository for VehiclePriceHistory entities
        /// </summary>
        IRepository<VehiclePriceHistory> VehiclePriceHistories { get; }

        /// <summary>
        /// Repository for PurchaseOrder entities
        /// </summary>
        IRepository<PurchaseOrder> PurchaseOrders { get; }

        /// <summary>
        /// Repository for purchase order line items (one per vehicle)
        /// </summary>
        IPurchaseOrderItemRepository PurchaseOrderItems { get; }

        /// <summary>
        /// Repository for Commission entities
        /// </summary>
        IRepository<Commission> Commissions { get; }

        /// <summary>
        /// Repository for CommissionRate entities
        /// </summary>
        IRepository<CommissionRate> CommissionRates { get; }

        /// <summary>
        /// Repository for ReturnRequest entities
        /// </summary>
        IRepository<ReturnRequest> ReturnRequests { get; }

        /// <summary>
        /// Repository for Payment entities
        /// </summary>
        IRepository<Payment> Payments { get; }

        /// <summary>
        /// Repository for AccountTransaction entities
        /// </summary>
        IRepository<AccountTransaction> AccountTransactions { get; }

        /// <summary>
        /// Repository for AuditLog entities
        /// </summary>
        IRepository<AuditLog> AuditLogs { get; }

        IRepository<Role> Roles { get; }
        IRepository<Dealership> Dealerships { get; }
        IRepository<SubDealer> SubDealers { get; }
        IRepository<RoleMenu> RoleMenus { get; }
        IRepository<UserOrgRole> UserOrgRoles { get; }
        IRepository<PaymentType> PaymentTypes { get; }
        IRepository<FinanceNameMaster> FinanceNames { get; }
        IRepository<StatusLookup> StatusLookups { get; }
        IRepository<DocumentTypeMaster> DocumentTypes { get; }
        IRepository<RtoLocationMaster> RtoLocations { get; }
        IRepository<VehicleBooking> VehicleBookings { get; }

        Task UpdateVehicleBookingStatusAsync(int bookingId, int bookingStatus, int? modifiedBy);

        /// <summary>
        /// Save all changes to database
        /// Commits the current transaction
        /// </summary>
        Task<int> SaveChangesAsync();

        /// <summary>
        /// Begin a new transaction
        /// </summary>
        Task BeginTransactionAsync();

        /// <summary>
        /// Commit current transaction
        /// </summary>
        Task CommitTransactionAsync();

        /// <summary>
        /// Rollback current transaction
        /// </summary>
        Task RollbackTransactionAsync();
    }
}
