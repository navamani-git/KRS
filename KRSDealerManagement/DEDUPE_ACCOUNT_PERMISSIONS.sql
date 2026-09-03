-- Remove duplicate AccountPermissions rows (keeps the most recently modified per AccountId + MenuKey).
;WITH ranked AS (
    SELECT
        PermissionId,
        ROW_NUMBER() OVER (
            PARTITION BY AccountId, MenuKey
            ORDER BY ModifiedDate DESC, PermissionId DESC
        ) AS rn
    FROM dbo.AccountPermissions
)
DELETE ap
FROM dbo.AccountPermissions ap
INNER JOIN ranked r ON r.PermissionId = ap.PermissionId
WHERE r.rn > 1;

PRINT 'Duplicate AccountPermissions removed.';
