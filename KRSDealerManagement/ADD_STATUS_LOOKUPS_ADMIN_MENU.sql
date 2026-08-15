-- Add Status Master menu for System Admin role
DECLARE @SystemAdmin INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'SYSTEM_ADMIN');

IF @SystemAdmin IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM dbo.RoleMenus
       WHERE RoleId = @SystemAdmin AND MenuKey = N'admin_status_lookups')
BEGIN
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@SystemAdmin, N'admin_status_lookups', N'Status Master', 1, 65);
END
GO

-- Also add Finance Names if missing (some DBs may not have it yet)
DECLARE @SystemAdmin2 INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'SYSTEM_ADMIN');

IF @SystemAdmin2 IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM dbo.RoleMenus
       WHERE RoleId = @SystemAdmin2 AND MenuKey = N'admin_finance_names')
BEGIN
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@SystemAdmin2, N'admin_finance_names', N'Finance Names', 1, 62);
END
GO
