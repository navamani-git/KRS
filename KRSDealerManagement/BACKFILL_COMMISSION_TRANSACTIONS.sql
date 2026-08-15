-- Backfill AccountTransactions for approved/paid commissions that were credited
-- but not logged (e.g. CK_TransactionType blocked type 7 before fix).

INSERT INTO dbo.AccountTransactions (
    SubdealerId,
    AccountId,
    TransactionType,
    Amount,
    BalanceBeforeTransaction,
    BalanceAfterTransaction,
    Description,
    Reason,
    ReferenceId,
    ReferenceType,
    ReferenceCommissionId,
    CreatedBy,
    InitiatedBy,
    CreatedDate
)
SELECT
    c.SubdealerId,
    sa.AccountId,
    7,
    ISNULL(c.ApprovedAmount, c.SubmittedAmount),
    ab.CurrentBalance - ISNULL(c.ApprovedAmount, c.SubmittedAmount),
    ab.CurrentBalance,
    N'Commission #' + CAST(c.CommissionId AS NVARCHAR(20)) + N' approved and credited',
    N'Commission #' + CAST(c.CommissionId AS NVARCHAR(20)) + N' approved and credited',
    c.CommissionId,
    N'Commission',
    c.CommissionId,
    ISNULL(c.ApprovedBy, c.SubmittedBy),
    ISNULL(c.ApprovedBy, c.SubmittedBy),
    ISNULL(c.ApprovedDate, c.ModifiedDate)
FROM dbo.CommissionHistory c
INNER JOIN dbo.SubdealerAccounts sa ON sa.SubdealerId = c.SubdealerId
INNER JOIN dbo.AccountBalance ab ON ab.SubdealerAccountId = sa.AccountId
WHERE c.CommissionStatus IN (1, 2)
  AND ISNULL(c.ApprovedAmount, c.SubmittedAmount) > 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.AccountTransactions t
      WHERE t.ReferenceCommissionId = c.CommissionId
         OR (t.ReferenceType = N'Commission' AND t.ReferenceId = c.CommissionId)
  );
GO
