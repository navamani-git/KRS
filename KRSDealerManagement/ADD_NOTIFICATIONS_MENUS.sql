-- Admin + subdealer notification menus
DECLARE @SystemAdmin INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'SYSTEM_ADMIN');
DECLARE @BranchMgr   INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'BRANCH_MANAGER');
DECLARE @Subdealer   INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'SUBDEALER');

IF @SystemAdmin IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @SystemAdmin AND MenuKey = N'admin_notifications')
BEGIN
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@SystemAdmin, N'admin_notifications', N'Notifications', 1, 15);
END

IF @BranchMgr IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @BranchMgr AND MenuKey = N'admin_notifications')
BEGIN
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@BranchMgr, N'admin_notifications', N'Notifications', 1, 15);
END

IF @Subdealer IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @Subdealer AND MenuKey = N'notifications')
BEGIN
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@Subdealer, N'notifications', N'Notifications', 1, 5);
END
GO

INSERT INTO dbo.AccountPermissions
    (AccountId, MenuKey, MenuName, IsAccessible, CanCreate, CanEdit, CanDelete, CanApprove, CreatedDate, ModifiedDate)
SELECT sa.AccountId, N'notifications', N'Notifications', 1, 0, 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM dbo.SubdealerAccounts sa
WHERE sa.IsActive = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.AccountPermissions ap
      WHERE ap.AccountId = sa.AccountId AND ap.MenuKey = N'notifications');
GO
