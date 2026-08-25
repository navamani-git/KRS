-- Admin transaction edit/delete: soft-delete flag + correction audit trail
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'IsDeleted' AND object_id = OBJECT_ID('AccountTransactions'))
BEGIN
    ALTER TABLE AccountTransactions ADD IsDeleted BIT NOT NULL CONSTRAINT DF_AccountTransactions_IsDeleted DEFAULT 0;
    PRINT 'Added IsDeleted to AccountTransactions';
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AccountTransactionCorrections')
BEGIN
    CREATE TABLE AccountTransactionCorrections (
        CorrectionId INT IDENTITY(1,1) PRIMARY KEY,
        TransactionId INT NOT NULL,
        AccountId INT NOT NULL,
        Action NVARCHAR(20) NOT NULL,
        OldSnapshot NVARCHAR(MAX) NOT NULL,
        NewSnapshot NVARCHAR(MAX) NULL,
        CorrectionReason NVARCHAR(500) NOT NULL,
        CorrectedBy INT NOT NULL,
        CorrectedByName NVARCHAR(200) NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_AccountTransactionCorrections_CreatedDate DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_AccountTransactionCorrections_AccountId ON AccountTransactionCorrections(AccountId);
    CREATE INDEX IX_AccountTransactionCorrections_TransactionId ON AccountTransactionCorrections(TransactionId);
    CREATE INDEX IX_AccountTransactionCorrections_CreatedDate ON AccountTransactionCorrections(CreatedDate DESC);
    PRINT 'Created AccountTransactionCorrections';
END
GO
