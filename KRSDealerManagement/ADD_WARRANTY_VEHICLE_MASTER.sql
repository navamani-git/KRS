-- Warranty-only vehicle master flag + staff warranty apply menus
IF COL_LENGTH('dbo.VehicleMasters', 'WarrantyOnly') IS NULL
BEGIN
    ALTER TABLE dbo.VehicleMasters
        ADD WarrantyOnly BIT NOT NULL CONSTRAINT DF_VehicleMasters_WarrantyOnly DEFAULT (0);
END
GO

DECLARE @SystemAdmin INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'SYSTEM_ADMIN');
DECLARE @BranchMgr   INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'BRANCH_MANAGER');

IF @BranchMgr IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @BranchMgr AND MenuKey = N'admin_warranty_apply')
BEGIN
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@BranchMgr, N'admin_warranty_apply', N'Apply Warranty (Staff)', 1, 47);
END

IF @SystemAdmin IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @SystemAdmin AND MenuKey = N'admin_warranty_apply')
BEGIN
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@SystemAdmin, N'admin_warranty_apply', N'Apply Warranty (Staff)', 1, 47);
END

IF @BranchMgr IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @BranchMgr AND MenuKey = N'admin_warranty_only_stock')
BEGIN
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@BranchMgr, N'admin_warranty_only_stock', N'Warranty-Only Vehicles', 1, 48);
END

IF @SystemAdmin IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @SystemAdmin AND MenuKey = N'admin_warranty_only_stock')
BEGIN
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@SystemAdmin, N'admin_warranty_only_stock', N'Warranty-Only Vehicles', 1, 48);
END
GO
