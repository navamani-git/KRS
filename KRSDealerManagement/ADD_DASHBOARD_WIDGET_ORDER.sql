-- Per-user dashboard pill/widget display order (comma-separated keys)
USE KRSDealerManagementDB;
GO

IF COL_LENGTH('Users', 'DashboardWidgetKeys') IS NULL
BEGIN
    ALTER TABLE Users ADD DashboardWidgetKeys NVARCHAR(2000) NULL;
END
GO

IF COL_LENGTH('Users', 'DashboardWidgetKeys') IS NOT NULL
BEGIN
    ALTER TABLE Users ALTER COLUMN DashboardWidgetKeys NVARCHAR(2000) NULL;
END
GO
