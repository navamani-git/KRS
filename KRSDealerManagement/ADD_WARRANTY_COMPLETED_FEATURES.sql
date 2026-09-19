-- Completed warranty + staff-on-behalf tracking
IF COL_LENGTH(N'dbo.WarrantyClaims', N'CompletedDate') IS NULL
BEGIN
    ALTER TABLE dbo.WarrantyClaims ADD CompletedDate DATETIME2 NULL;
END
GO

IF COL_LENGTH(N'dbo.WarrantyClaims', N'SubdealerPartReceivedStaffUserId') IS NULL
BEGIN
    ALTER TABLE dbo.WarrantyClaims ADD SubdealerPartReceivedStaffUserId INT NULL;
END
GO

IF COL_LENGTH(N'dbo.WarrantyClaims', N'DefectiveHandoverStaffUserId') IS NULL
BEGIN
    ALTER TABLE dbo.WarrantyClaims ADD DefectiveHandoverStaffUserId INT NULL;
END
GO

UPDATE dbo.WarrantyClaims
SET CompletedDate = ModifiedDate
WHERE Status = 7 AND CompletedDate IS NULL;
GO

DECLARE @BranchMgr INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'BRANCH_MANAGER');
DECLARE @Subdealer INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'SUBDEALER');

IF @BranchMgr IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @BranchMgr AND MenuKey = N'admin_warranty_completed')
BEGIN
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@BranchMgr, N'admin_warranty_completed', N'Completed Warranty', 1, 87);
END

IF @Subdealer IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @Subdealer AND MenuKey = N'my_completed_warranty')
BEGIN
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@Subdealer, N'my_completed_warranty', N'Completed Warranty', 1, 47);
END
GO

INSERT INTO dbo.AccountPermissions
    (AccountId, MenuKey, MenuName, IsAccessible, CanCreate, CanEdit, CanDelete, CanApprove, CreatedDate, ModifiedDate)
SELECT sa.AccountId, N'my_completed_warranty', N'Completed Warranty', 1, 0, 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM dbo.SubdealerAccounts sa
WHERE sa.IsActive = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.AccountPermissions ap
      WHERE ap.AccountId = sa.AccountId AND ap.MenuKey = N'my_completed_warranty');
GO
