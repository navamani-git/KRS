-- Vehicle Aging: subsidy ID date + admin/staff subsidy-complete approval.
-- Also grants the Vehicle Aging menu to staff roles that already have booking menus,
-- and to existing subdealer accounts.

IF COL_LENGTH('dbo.VehicleBookings', 'SubsidyIdDate') IS NULL
    ALTER TABLE dbo.VehicleBookings ADD SubsidyIdDate DATETIME2 NULL;
IF COL_LENGTH('dbo.VehicleBookings', 'SubsidyCompletedApproved') IS NULL
    ALTER TABLE dbo.VehicleBookings ADD SubsidyCompletedApproved BIT NOT NULL CONSTRAINT DF_VB_SubsidyApproved DEFAULT(0);
IF COL_LENGTH('dbo.VehicleBookings', 'SubsidyCompletedApprovedDate') IS NULL
    ALTER TABLE dbo.VehicleBookings ADD SubsidyCompletedApprovedDate DATETIME2 NULL;
IF COL_LENGTH('dbo.VehicleBookings', 'SubsidyCompletedApprovedBy') IS NULL
    ALTER TABLE dbo.VehicleBookings ADD SubsidyCompletedApprovedBy INT NULL;
GO

UPDATE dbo.VehicleBookings
SET SubsidyIdDate = COALESCE(SubsidyIdDate, ModifiedDate, CreatedDate)
WHERE SubsidyId IS NOT NULL
  AND LTRIM(RTRIM(SubsidyId)) <> N''
  AND SubsidyIdDate IS NULL;
GO

DECLARE @MenuKey NVARCHAR(80) = N'admin_vehicle_aging';
DECLARE @MenuName NVARCHAR(150) = N'Vehicle Aging';
DECLARE @SubKey NVARCHAR(80) = N'vehicle_aging';

INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
SELECT DISTINCT rm.RoleId, @MenuKey, @MenuName, 1, 88
FROM dbo.RoleMenus rm
WHERE rm.MenuKey IN (N'admin_vehicle_bookings', N'admin_showroom_stock', N'admin_vehicles')
  AND NOT EXISTS (
      SELECT 1 FROM dbo.RoleMenus x
      WHERE x.RoleId = rm.RoleId AND x.MenuKey = @MenuKey);

DECLARE @Subdealer INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'SUBDEALER');
IF @Subdealer IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @Subdealer AND MenuKey = @SubKey)
BEGIN
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@Subdealer, @SubKey, @MenuName, 1, 36);
END
GO

INSERT INTO dbo.AccountPermissions
    (AccountId, MenuKey, MenuName, IsAccessible, CanCreate, CanEdit, CanDelete, CanApprove, CreatedDate, ModifiedDate)
SELECT sa.AccountId, N'vehicle_aging', N'Vehicle Aging', 1, 1, 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM dbo.SubdealerAccounts sa
WHERE sa.IsActive = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.AccountPermissions ap
      WHERE ap.AccountId = sa.AccountId AND ap.MenuKey = N'vehicle_aging');
GO
