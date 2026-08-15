-- Ensure Staff Users menu exists for System Admin
DECLARE @Admin INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'SYSTEM_ADMIN');

IF @Admin IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @Admin AND MenuKey = N'admin_staff_users')
BEGIN
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@Admin, N'admin_staff_users', N'Staff Users', 1, 30);
END
GO
