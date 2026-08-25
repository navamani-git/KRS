-- Repair Sankagiri (AccountId=17) after admin delete recalc bug (started from 0 instead of InitialBalance 1000000)
-- Correct CurrentBalance after deleted commission txn #25: 189000; Available = 189000 - 119000 reserved = 70000

UPDATE AccountTransactions SET BalanceAfterTransaction = 1000000.00 WHERE TransactionId = 12;
UPDATE AccountTransactions SET BalanceAfterTransaction = 524000.00 WHERE TransactionId = 13;
UPDATE AccountTransactions SET BalanceAfterTransaction = 624000.00 WHERE TransactionId = 14;
UPDATE AccountTransactions SET BalanceAfterTransaction = 743000.00 WHERE TransactionId = 15;
UPDATE AccountTransactions SET BalanceAfterTransaction = 755345.00 WHERE TransactionId = 20;
UPDATE AccountTransactions SET BalanceAfterTransaction = 764345.00 WHERE TransactionId = 21;
UPDATE AccountTransactions SET BalanceAfterTransaction = 764345.00 WHERE TransactionId = 22;
UPDATE AccountTransactions SET BalanceAfterTransaction = 564345.00 WHERE TransactionId = 23;
UPDATE AccountTransactions SET BalanceAfterTransaction = 574345.00 WHERE TransactionId = 24;
UPDATE AccountTransactions SET BalanceAfterTransaction = 574345.00 WHERE TransactionId = 26;
UPDATE AccountTransactions SET BalanceAfterTransaction = 474345.00 WHERE TransactionId = 27;
UPDATE AccountTransactions SET BalanceAfterTransaction = 374345.00 WHERE TransactionId = 28;
UPDATE AccountTransactions SET BalanceAfterTransaction = 574345.00 WHERE TransactionId = 29;
UPDATE AccountTransactions SET BalanceAfterTransaction = 474345.00 WHERE TransactionId = 30;
UPDATE AccountTransactions SET BalanceAfterTransaction = 374345.00 WHERE TransactionId = 31;
UPDATE AccountTransactions SET BalanceAfterTransaction = 394345.00 WHERE TransactionId = 32;
UPDATE AccountTransactions SET BalanceAfterTransaction = 394000.00 WHERE TransactionId = 33;
UPDATE AccountTransactions SET BalanceAfterTransaction = 399000.00 WHERE TransactionId = 34;
UPDATE AccountTransactions SET BalanceAfterTransaction = 413000.00 WHERE TransactionId = 41;
UPDATE AccountTransactions SET BalanceAfterTransaction = 427000.00 WHERE TransactionId = 42;
UPDATE AccountTransactions SET BalanceAfterTransaction = 441000.00 WHERE TransactionId = 43;
UPDATE AccountTransactions SET BalanceAfterTransaction = 546000.00 WHERE TransactionId = 47;
UPDATE AccountTransactions SET BalanceAfterTransaction = 441000.00 WHERE TransactionId = 48;
UPDATE AccountTransactions SET BalanceAfterTransaction = 427000.00 WHERE TransactionId = 49;
UPDATE AccountTransactions SET BalanceAfterTransaction = 308000.00 WHERE TransactionId = 51;
UPDATE AccountTransactions SET BalanceAfterTransaction = 189000.00 WHERE TransactionId = 52;

UPDATE AccountBalance
SET CurrentBalance = 189000.00,
    AvailableBalance = 70000.00,
    ModifiedDate = SYSUTCDATETIME()
WHERE AccountId = 17 OR SubdealerAccountId = 17;

PRINT 'Sankagiri account 17 balances repaired.';
