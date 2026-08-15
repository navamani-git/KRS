/*
  LOCAL DB ONLY — seed master/lookup tables after truncate.
  Status Master (StatusLookups) + admin menu.
*/
SET NOCOUNT ON;

------------------------------------------------------------
-- Status Master — VEHICLE (unified 14), PAYMENT, COMMISSION
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.StatusLookups WHERE Category = N'PAYMENT')
INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder) VALUES
 (N'PAYMENT', 0, N'PENDING',  N'Pending',  N'bg-warning text-dark', 1),
 (N'PAYMENT', 1, N'APPROVED', N'Approved', N'bg-success', 2),
 (N'PAYMENT', 2, N'REJECTED', N'Rejected', N'bg-danger', 3);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.StatusLookups WHERE Category = N'COMMISSION')
INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder) VALUES
 (N'COMMISSION', 0, N'AWAITING_APPROVAL', N'Awaiting Approval', N'bg-warning text-dark', 1),
 (N'COMMISSION', 1, N'APPROVED',          N'Approved',          N'bg-info', 2),
 (N'COMMISSION', 2, N'PAID',              N'Paid',              N'bg-success', 3),
 (N'COMMISSION', 3, N'REJECTED',          N'Rejected',          N'bg-danger', 4);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.StatusLookups WHERE Category = N'VEHICLE')
INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder, IsActive) VALUES
 (N'VEHICLE',  1, N'SUBMITTED',           N'Submitted',           N'bg-warning text-dark', 10, 1),
 (N'VEHICLE',  2, N'APPROVED_BY_DEALER',  N'Approved By Dealer',  N'bg-success', 20, 1),
 (N'VEHICLE',  3, N'REJECTED_BY_DEALER',  N'Rejected By Dealer',  N'bg-danger', 30, 1),
 (N'VEHICLE',  4, N'RETURN_REQUESTED',    N'Return Requested',    N'bg-warning text-dark', 40, 1),
 (N'VEHICLE',  5, N'RETURN_APPROVED',     N'Return Approved',     N'bg-info', 50, 1),
 (N'VEHICLE',  6, N'RETURN_CANCELLED',    N'Return Cancelled',    N'bg-secondary', 60, 1),
 (N'VEHICLE',  7, N'BOOKED_TO_CUSTOMER',  N'Booked to Customer',  N'bg-primary', 70, 1),
 (N'VEHICLE',  8, N'PAPER_RECEIVED',      N'Paper Received',      N'bg-info', 80, 1),
 (N'VEHICLE',  9, N'INVOICED',            N'Invoiced',            N'bg-info', 90, 1),
 (N'VEHICLE', 10, N'INSURANCE_CREATED',   N'Insurance Created',   N'bg-info', 100, 1),
 (N'VEHICLE', 11, N'RTO_REQUESTED',       N'RTO Requested',       N'bg-warning text-dark', 110, 1),
 (N'VEHICLE', 12, N'REGISTERED',          N'Registered',          N'bg-warning text-dark', 120, 1),
 (N'VEHICLE', 13, N'SUBSIDY_ID_CREATED',  N'Subsidy ID Created',  N'bg-warning text-dark', 130, 1),
 (N'VEHICLE', 14, N'DELIVERED',           N'Delivered',           N'bg-success', 140, 1);
GO

------------------------------------------------------------
-- Admin menu: Status Master
------------------------------------------------------------
DECLARE @SystemAdmin INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'SYSTEM_ADMIN');

IF @SystemAdmin IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @SystemAdmin AND MenuKey = N'admin_status_lookups')
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@SystemAdmin, N'admin_status_lookups', N'Status Master', 1, 65);
GO

PRINT '=== Master data seeded ===';
SELECT Category, COUNT(*) AS StatusCount
FROM dbo.StatusLookups
GROUP BY Category
ORDER BY Category;
GO
