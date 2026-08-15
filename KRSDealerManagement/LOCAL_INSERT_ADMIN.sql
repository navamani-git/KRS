-- LOCAL DB ONLY — create admin user (run right after full truncate, before HIERARCHY_SCHEMA.sql)
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'admin')
BEGIN
    INSERT INTO dbo.Users (Username, Email, PasswordHash, FirstName, LastName, UserRole, PhoneNumber, IsActive, CreatedDate, ModifiedDate)
    VALUES (N'admin', N'admin@krsdealers.com', N'Admin@123', N'Ampere', N'Admin', 1, N'9876543210', 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    PRINT 'Admin user created.';
END
GO
