-- Grant Balances menu to Dealer Branch Manager (and ensure vehicles/bookings menus exist)
DECLARE @BranchMgr INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'BRANCH_MANAGER');

IF @BranchMgr IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @BranchMgr AND MenuKey = N'admin_balances')
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@BranchMgr, N'admin_balances', N'Balances', 1, 80);

IF @BranchMgr IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @BranchMgr AND MenuKey = N'admin_vehicles')
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@BranchMgr, N'admin_vehicles', N'Subdealer Vehicles', 1, 85);

IF @BranchMgr IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @BranchMgr AND MenuKey = N'admin_vehicle_bookings')
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@BranchMgr, N'admin_vehicle_bookings', N'Vehicle Bookings', 1, 86);

PRINT 'Branch manager menus updated.';
