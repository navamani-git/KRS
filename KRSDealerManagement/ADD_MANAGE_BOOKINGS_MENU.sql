-- Manage Vehicles / Booked to Customer menus (idempotent)
DECLARE @Admin INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'SYSTEM_ADMIN');
DECLARE @Mgr   INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'BRANCH_MANAGER');

IF @Admin IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @Admin AND MenuKey = N'admin_vehicle_bookings')
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@Admin, N'admin_vehicle_bookings', N'Vehicle Booking Process', 1, 86);

IF @Mgr IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @Mgr AND MenuKey = N'admin_vehicle_bookings')
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@Mgr, N'admin_vehicle_bookings', N'Vehicle Booking Process', 1, 86);

IF @Admin IS NOT NULL
    UPDATE dbo.RoleMenus
    SET MenuName = N'Vehicle Booking Process'
    WHERE RoleId = @Admin AND MenuKey = N'admin_vehicle_bookings';

IF @Mgr IS NOT NULL
    UPDATE dbo.RoleMenus
    SET MenuName = N'Vehicle Booking Process'
    WHERE RoleId = @Mgr AND MenuKey = N'admin_vehicle_bookings';

IF @Admin IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @Admin AND MenuKey = N'admin_booked_to_customer')
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@Admin, N'admin_booked_to_customer', N'Booked to Customer', 1, 85);

IF @Mgr IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @Mgr AND MenuKey = N'admin_booked_to_customer')
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@Mgr, N'admin_booked_to_customer', N'Booked to Customer', 1, 85);

IF @Mgr IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @Mgr AND MenuKey = N'admin_balances')
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@Mgr, N'admin_balances', N'Balances', 1, 80);

IF @Mgr IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @Mgr AND MenuKey = N'admin_vehicles')
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@Mgr, N'admin_vehicles', N'Subdealer Vehicles', 1, 84);

IF @Admin IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @Admin AND MenuKey = N'admin_chassis_history')
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@Admin, N'admin_chassis_history', N'Chassis History', 1, 87);

PRINT 'Manage Vehicles menus updated.';
GO
