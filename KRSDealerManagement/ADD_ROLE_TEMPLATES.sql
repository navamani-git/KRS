/* Admin-defined role templates with default menu permissions. */

IF OBJECT_ID('dbo.RoleTemplates', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RoleTemplates (
        RoleTemplateId   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TemplateCode     NVARCHAR(50) NOT NULL,
        TemplateName     NVARCHAR(120) NOT NULL,
        Description      NVARCHAR(500) NULL,
        LegacyUserRole   INT NOT NULL CONSTRAINT DF_RoleTemplates_LegacyRole DEFAULT(4),
        IsActive         BIT NOT NULL CONSTRAINT DF_RoleTemplates_Active DEFAULT(1),
        CreatedBy        INT NULL,
        CreatedDate      DATETIME2 NOT NULL CONSTRAINT DF_RoleTemplates_Created DEFAULT(SYSUTCDATETIME()),
        ModifiedDate     DATETIME2 NOT NULL CONSTRAINT DF_RoleTemplates_Modified DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT UQ_RoleTemplates_Code UNIQUE (TemplateCode)
    );
    PRINT 'Created RoleTemplates.';
END

IF OBJECT_ID('dbo.RoleTemplateMenus', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RoleTemplateMenus (
        RoleTemplateMenuId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RoleTemplateId     INT NOT NULL,
        MenuKey            NVARCHAR(100) NOT NULL,
        IsReadOnly         BIT NOT NULL CONSTRAINT DF_RoleTemplateMenus_ReadOnly DEFAULT(0),
        SortOrder          INT NOT NULL CONSTRAINT DF_RoleTemplateMenus_Sort DEFAULT(0),
        CONSTRAINT FK_RoleTemplateMenus_Template FOREIGN KEY (RoleTemplateId)
            REFERENCES dbo.RoleTemplates(RoleTemplateId) ON DELETE CASCADE,
        CONSTRAINT UQ_RoleTemplateMenus UNIQUE (RoleTemplateId, MenuKey)
    );
    CREATE INDEX IX_RoleTemplateMenus_Template ON dbo.RoleTemplateMenus(RoleTemplateId, SortOrder);
    PRINT 'Created RoleTemplateMenus.';
END

GO

DECLARE @Admin INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'SYSTEM_ADMIN');
IF @Admin IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @Admin AND MenuKey = N'admin_role_templates')
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, IsReadOnly, SortOrder)
    VALUES (@Admin, N'admin_role_templates', N'Role Templates', 1, 0, 24);

GO
