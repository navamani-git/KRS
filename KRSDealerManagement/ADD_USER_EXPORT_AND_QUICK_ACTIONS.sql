-- Per-user Excel export toggle and dashboard quick-action preferences
USE KRSDealerManagementDB;
GO

IF COL_LENGTH('Users', 'CanExport') IS NULL
BEGIN
    ALTER TABLE Users ADD CanExport BIT NOT NULL CONSTRAINT DF_Users_CanExport DEFAULT (1);
END
GO

IF COL_LENGTH('Users', 'QuickActionKeys') IS NULL
BEGIN
    ALTER TABLE Users ADD QuickActionKeys NVARCHAR(2000) NULL;
END
GO

IF COL_LENGTH('Users', 'QuickActionKeys') IS NOT NULL
BEGIN
    ALTER TABLE Users ALTER COLUMN QuickActionKeys NVARCHAR(2000) NULL;
END
GO
