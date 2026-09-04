using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Infrastructure.Data;
using System.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    /// <summary>
    /// Unit of Work pattern - coordinates all repositories
    /// Manages database transactions and SaveChanges
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbTransaction _transaction;

        // Repository instances
        private IRepository<User> _users;
        private IRepository<SubdealerAccount> _subdealerAccounts;
        private IRepository<AccountPermission> _accountPermissions;
        private IRepository<AccountBalance> _accountBalances;
        private IRepository<Vehicle> _vehicles;
        private IVehicleMasterRepository _vehicleMasters;
        private ISubdealerVehicleHistoryRepository _subdealerVehicleHistories;
        private IRepository<VehicleModel> _vehicleModels;
        private IRepository<VehicleColor> _vehicleColors;
        private IVehicleModelColorRepository _vehicleModelColors;
        private IRepository<VehiclePriceHistory> _vehiclePrices;
        private IRepository<PurchaseOrder> _purchaseOrders;
        private IPurchaseOrderItemRepository _purchaseOrderItems;
        private IRepository<Commission> _commissions;
        private IRepository<CommissionRate> _commissionRates;
        private IRepository<ReturnRequest> _returnRequests;
        private IRepository<Payment> _payments;
        private IRepository<AccountTransaction> _accountTransactions;
        private IRepository<AccountTransactionCorrection> _accountTransactionCorrections;
        private IRepository<AuditLog> _auditLogs;
        private IRepository<Role> _roles;
        private IRepository<Dealership> _dealerships;
        private IRepository<SubDealer> _subDealers;
        private IRepository<RoleMenu> _roleMenus;
        private IRoleTemplateRepository _roleTemplates;
        private IRepository<UserOrgRole> _userOrgRoles;
        private IRepository<PaymentType> _paymentTypes;
        private IRepository<FinanceNameMaster> _financeNames;
        private IRepository<StatusLookup> _statusLookups;
        private IRepository<DocumentTypeMaster> _documentTypes;
        private IRepository<RtoDistrictMaster> _rtoDistricts;
        private IRepository<RtoLocationMaster> _rtoLocations;
        private IRepository<VehicleBooking> _vehicleBookings;
        private IRepository<WarrantyPartMaster> _warrantyParts;
        private IRepository<WarrantyClaim> _warrantyClaims;
        private IRepository<WarrantyClaimServiceEntry> _warrantyClaimServiceEntries;
        private IRepository<WarrantyClaimAttachment> _warrantyClaimAttachments;
        private IRepository<WarrantyClaimStatusHistory> _warrantyClaimStatusHistories;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lazy-load repository instances
        public IRepository<User> Users => _users ??= new UserRepository(_context);
        public IRepository<SubdealerAccount> SubdealerAccounts => _subdealerAccounts ??= new SubdealerAccountRepository(_context);
        public IRepository<AccountPermission> AccountPermissions => _accountPermissions ??= new AccountPermissionRepository(_context);
        public IRepository<AccountBalance> AccountBalances => _accountBalances ??= new AccountBalanceRepository(_context);
        public IRepository<Vehicle> Vehicles => _vehicles ??= new VehicleRepository(_context);
        public IVehicleMasterRepository VehicleMasters => _vehicleMasters ??= new VehicleMasterRepository(_context);
        public ISubdealerVehicleHistoryRepository SubdealerVehicleHistories => _subdealerVehicleHistories ??= new SubdealerVehicleHistoryRepository(_context);
        public IRepository<VehicleModel> VehicleModels => _vehicleModels ??= new VehicleModelRepository(_context);
        public IRepository<VehicleColor> VehicleColors => _vehicleColors ??= new VehicleColorRepository(_context);
        public IVehicleModelColorRepository VehicleModelColors => _vehicleModelColors ??= new VehicleModelColorRepository(_context);
        public IRepository<VehiclePriceHistory> VehiclePriceHistories => _vehiclePrices ??= new VehiclePriceHistoryRepository(_context);
        public IRepository<PurchaseOrder> PurchaseOrders => _purchaseOrders ??= new PurchaseOrderRepository(_context);
        public IPurchaseOrderItemRepository PurchaseOrderItems => _purchaseOrderItems ??= new PurchaseOrderItemRepository(_context);
        public IRepository<Commission> Commissions => _commissions ??= new CommissionRepository(_context);
        public IRepository<CommissionRate> CommissionRates => _commissionRates ??= new CommissionRateRepository(_context);
        public IRepository<ReturnRequest> ReturnRequests => _returnRequests ??= new ReturnRequestRepository(_context);
        public IRepository<Payment> Payments => _payments ??= new PaymentRepository(_context);
        public IRepository<AccountTransaction> AccountTransactions => _accountTransactions ??= new AccountTransactionRepository(_context);
        public IRepository<AccountTransactionCorrection> AccountTransactionCorrections => _accountTransactionCorrections ??= new AccountTransactionCorrectionRepository(_context);
        public IRepository<AuditLog> AuditLogs => _auditLogs ??= new AuditLogRepository(_context);
        public IRepository<Role> Roles => _roles ??= new Repository<Role>(_context, "Roles", "RoleId");
        public IRepository<Dealership> Dealerships => _dealerships ??= new Repository<Dealership>(_context, "Dealerships", "DealershipId");
        public IRepository<SubDealer> SubDealers => _subDealers ??= new Repository<SubDealer>(_context, "SubDealers", "SubDealerId");
        public IRepository<RoleMenu> RoleMenus => _roleMenus ??= new Repository<RoleMenu>(_context, "RoleMenus", "RoleMenuId");
        public IRoleTemplateRepository RoleTemplates => _roleTemplates ??= new RoleTemplateRepository(_context);
        public IRepository<UserOrgRole> UserOrgRoles => _userOrgRoles ??= new Repository<UserOrgRole>(_context, "UserOrgRoles", "UserOrgRoleId");
        public IRepository<PaymentType> PaymentTypes => _paymentTypes ??= new Repository<PaymentType>(_context, "PaymentTypes", "PaymentTypeId");
        public IRepository<FinanceNameMaster> FinanceNames => _financeNames ??= new Repository<FinanceNameMaster>(_context, "FinanceNames", "FinanceNameId");
        public IRepository<StatusLookup> StatusLookups => _statusLookups ??= new Repository<StatusLookup>(_context, "StatusLookups", "StatusLookupId");
        public IRepository<DocumentTypeMaster> DocumentTypes => _documentTypes ??= new Repository<DocumentTypeMaster>(_context, "DocumentTypeMasters", "DocumentTypeId");
        public IRepository<RtoDistrictMaster> RtoDistricts => _rtoDistricts ??= new Repository<RtoDistrictMaster>(_context, "RtoDistrictMasters", "RtoDistrictId");
        public IRepository<RtoLocationMaster> RtoLocations => _rtoLocations ??= new Repository<RtoLocationMaster>(_context, "RtoLocationMasters", "RtoLocationId");
        public IRepository<VehicleBooking> VehicleBookings => _vehicleBookings ??= new VehicleBookingRepository(_context);
        public IRepository<WarrantyPartMaster> WarrantyParts => _warrantyParts ??= new Repository<WarrantyPartMaster>(_context, "WarrantyParts", "WarrantyPartId");
        public IRepository<WarrantyClaim> WarrantyClaims => _warrantyClaims ??= new WarrantyClaimRepository(_context);
        public IRepository<WarrantyClaimServiceEntry> WarrantyClaimServiceEntries => _warrantyClaimServiceEntries ??= new Repository<WarrantyClaimServiceEntry>(_context, "WarrantyClaimServiceEntries", "ServiceEntryId");
        public IRepository<WarrantyClaimAttachment> WarrantyClaimAttachments => _warrantyClaimAttachments ??= new Repository<WarrantyClaimAttachment>(_context, "WarrantyClaimAttachments", "AttachmentId");
        public IRepository<WarrantyClaimStatusHistory> WarrantyClaimStatusHistories => _warrantyClaimStatusHistories ??= new Repository<WarrantyClaimStatusHistory>(_context, "WarrantyClaimStatusHistory", "HistoryId");

        public async Task UpdateVehicleBookingStatusAsync(int bookingId, int vehicleStatus, int? modifiedBy)
        {
            if (_vehicleBookings is VehicleBookingRepository repo)
                await repo.UpdateStatusAsync(bookingId, vehicleStatus, modifiedBy);

            var booking = await VehicleBookings.GetByIdAsync(bookingId);
            if (booking != null)
            {
                var vehicle = await Vehicles.GetByIdAsync(booking.VehicleId);
                if (vehicle != null)
                {
                    vehicle.Status = vehicleStatus;
                    vehicle.ModifiedBy = modifiedBy;
                    vehicle.ModifiedDate = DateTime.UtcNow;
                    await Vehicles.UpdateAsync(vehicle);
                }
            }
        }

        public async Task<int> SaveChangesAsync()
        {
            // In Dapper, changes are immediately persisted, so this is a no-op
            // Kept for interface compatibility
            await Task.CompletedTask;
            return 0;
        }

        public async Task BeginTransactionAsync()
        {
            _context.BeginScopedTransaction();
            await Task.CompletedTask;
        }

        public async Task CommitTransactionAsync()
        {
            _context.CommitScopedTransaction();
            _transaction = null;
            await Task.CompletedTask;
        }

        public async Task RollbackTransactionAsync()
        {
            _context.RollbackScopedTransaction();
            _transaction = null;
            await Task.CompletedTask;
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<IUnitOfWork, Task<T>> operation)
        {
            return await _context.ExecuteInTransactionAsync(async (conn, trans) =>
            {
                return await operation(this);
            });
        }

        public void Dispose()
        {
            _transaction?.Dispose();
        }
    }
}
