-- =====================================================
-- UPDATE PASSWORDS TO PLAIN TEXT FOR LOGIN
-- Run this in SSMS against KRSDealerManagementDB
-- =====================================================
-- 
-- Our LoginCommandHandler supports both:
--   1. ASP.NET Identity PBKDF2 hashes (starts with AQAA)
--   2. Plain text passwords (for dev accounts)
--
-- The hash in DATABASE_SETUP.sql is a DUMMY/FAKE hash.
-- This script resets all passwords to plain text so login works.
-- =====================================================

USE KRSDealerManagementDB;
GO

-- Reset Admin password
UPDATE [User] 
SET PasswordHash = 'Admin@123', ModifiedDate = GETUTCDATE()
WHERE Username = 'admin';

-- Reset all Subdealer passwords
UPDATE [User] 
SET PasswordHash = 'Subdealers@123', ModifiedDate = GETUTCDATE()
WHERE UserRole = 2;

-- Verify
SELECT UserId, Username, UserRole, 
       CASE UserRole WHEN 1 THEN 'Admin' ELSE 'Subdealer' END AS RoleName,
       PasswordHash AS Password,
       IsActive
FROM [User]
ORDER BY UserRole, Username;

PRINT '';
PRINT '=== Login Credentials ===';
PRINT 'Admin    -> Username: admin            | Password: Admin@123';
PRINT 'Subdealer-> Username: subdealer_001    | Password: Subdealers@123';
PRINT 'Subdealer-> Username: subdealer_002    | Password: Subdealers@123';
PRINT '                  ... up to subdealer_028';
