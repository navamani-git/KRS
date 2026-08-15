-- =====================================================
-- FIX DATABASE ACCESS
-- Run this in SSMS connected as SA or sysadmin
-- =====================================================

USE master;
GO

-- Step 1: Enable SA login and set password
ALTER LOGIN sa ENABLE;
GO
ALTER LOGIN sa WITH PASSWORD = 'KRS@Admin123';
GO

-- Step 2: Enable SQL Server + Windows Authentication mode
EXEC xp_instance_regwrite 
    N'HKEY_LOCAL_MACHINE', 
    N'Software\Microsoft\MSSQLServer\MSSQLServer',
    N'LoginMode', 
    REG_DWORD, 
    2;  -- 2 = Mixed Mode (SQL + Windows)
GO

PRINT '=== DONE ===';
PRINT 'SA login enabled with password: KRS@Admin123';
PRINT '';
PRINT 'IMPORTANT: Restart SQL Server service now!';
PRINT '  - Open Services (services.msc)';
PRINT '  - Find: SQL Server (SQLEXPRESS)';
PRINT '  - Right-click -> Restart';
PRINT '';
PRINT 'Then update appsettings.json connection string:';
PRINT 'User Id=sa;Password=KRS@Admin123';
GO
