/*
  Remove leftover inactive Staff Roles: legacy BRANCH_MANAGER and FINANCE_ADMIN.
  Those were deactivated (not deleted) by ADD_DYNAMIC_ROLES.sql, so they still
  appear on Staff Roles when status = Inactive / All.

  Safe: does not touch SYSTEM_ADMIN, SUBDEALER, or dealership staff roles.
  Run anytime. No backup of transactional data required.
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.UserOrgRoles', N'U') IS NOT NULL
BEGIN
    DELETE uor
    FROM dbo.UserOrgRoles uor
    INNER JOIN dbo.Roles r ON r.RoleId = uor.RoleId
    WHERE r.RoleCode IN (N'BRANCH_MANAGER', N'FINANCE_ADMIN');
END

IF OBJECT_ID(N'dbo.RoleMenus', N'U') IS NOT NULL
BEGIN
    DELETE rm
    FROM dbo.RoleMenus rm
    INNER JOIN dbo.Roles r ON r.RoleId = rm.RoleId
    WHERE r.RoleCode IN (N'BRANCH_MANAGER', N'FINANCE_ADMIN');
END

DELETE FROM dbo.Roles
WHERE RoleCode IN (N'BRANCH_MANAGER', N'FINANCE_ADMIN');

PRINT 'Removed BRANCH_MANAGER and FINANCE_ADMIN if they existed.';

SELECT RoleId, RoleCode, RoleName, IsActive, IsSystemRole
FROM dbo.Roles
ORDER BY RoleId;
GO
