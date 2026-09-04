-- Warranty menus for staff roles and subdealer accounts
DECLARE @SystemAdmin INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'SYSTEM_ADMIN');
DECLARE @BranchMgr   INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'BRANCH_MANAGER');
DECLARE @Subdealer   INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'SUBDEALER');

-- Staff: warranty claims (branch manager + any role using RoleMenus seed)
IF @BranchMgr IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @BranchMgr AND MenuKey = N'admin_warranty_claims')
BEGIN
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@BranchMgr, N'admin_warranty_claims', N'Warranty Claims', 1, 86);
END

-- Subdealer role template menus
IF @Subdealer IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @Subdealer AND MenuKey = N'my_warranty_claims')
BEGIN
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@Subdealer, N'my_warranty_claims', N'My Warranty Claims', 1, 45);
END

IF @Subdealer IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @Subdealer AND MenuKey = N'warranty_apply')
BEGIN
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@Subdealer, N'warranty_apply', N'Apply Warranty / Campaign', 1, 46);
END
GO

-- Grant warranty menus to existing subdealer accounts
INSERT INTO dbo.AccountPermissions
    (AccountId, MenuKey, MenuName, IsAccessible, CanCreate, CanEdit, CanDelete, CanApprove, CreatedDate, ModifiedDate)
SELECT sa.AccountId, N'my_warranty_claims', N'My Warranty Claims', 1, 1, 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM dbo.SubdealerAccounts sa
WHERE sa.IsActive = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.AccountPermissions ap
      WHERE ap.AccountId = sa.AccountId AND ap.MenuKey = N'my_warranty_claims');
GO

INSERT INTO dbo.AccountPermissions
    (AccountId, MenuKey, MenuName, IsAccessible, CanCreate, CanEdit, CanDelete, CanApprove, CreatedDate, ModifiedDate)
SELECT sa.AccountId, N'warranty_apply', N'Apply Warranty / Campaign', 1, 1, 1, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM dbo.SubdealerAccounts sa
WHERE sa.IsActive = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.AccountPermissions ap
      WHERE ap.AccountId = sa.AccountId AND ap.MenuKey = N'warranty_apply');
GO
