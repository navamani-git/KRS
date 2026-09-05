-- Hide Role Templates from admin menus.
-- Safe for live data: does NOT update Roles, Users, UserOrgRoles,
-- and does NOT drop RoleTemplates / RoleTemplateMenus tables.
-- Existing RoleTemplateCode values stay as-is so login mapping is unchanged.

DELETE FROM dbo.RoleMenus
WHERE MenuKey = N'admin_role_templates';

PRINT 'Role Templates menu removed. Roles.RoleTemplateCode and user mappings were not changed.';
