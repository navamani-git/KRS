-- Prompt items 4–9: return dates, staff statement restriction
USE KRSDealerManagementDB;
GO

IF COL_LENGTH('ReturnRequests', 'ReturnDate') IS NULL
BEGIN
    ALTER TABLE dbo.ReturnRequests ADD ReturnDate DATE NULL;
    PRINT 'Added ReturnRequests.ReturnDate.';
END
GO

IF COL_LENGTH('ReturnRequests', 'ReturnReceivedDate') IS NULL
BEGIN
    ALTER TABLE dbo.ReturnRequests ADD ReturnReceivedDate DATE NULL;
    PRINT 'Added ReturnRequests.ReturnReceivedDate.';
END
GO

IF COL_LENGTH('Users', 'CanViewStatement') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD CanViewStatement BIT NOT NULL CONSTRAINT DF_Users_CanViewStatement DEFAULT (1);
    PRINT 'Added Users.CanViewStatement.';
END
GO

PRINT 'Prompt items 4–9 DB columns ready.';
