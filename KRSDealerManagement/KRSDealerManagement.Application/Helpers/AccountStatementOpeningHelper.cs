using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Helpers
{
    public static class AccountStatementOpeningHelper
    {
        public static bool IsInitialBalanceTransaction(AccountTransactionDto transaction)
        {
            if (string.Equals(transaction.ReferenceType, "AccountCreation", StringComparison.OrdinalIgnoreCase))
                return true;

            return string.Equals(transaction.CategoryLabel, "Opening Balance", StringComparison.OrdinalIgnoreCase)
                   || (!string.IsNullOrWhiteSpace(transaction.Reason)
                       && transaction.Reason.Contains("Initial balance", StringComparison.OrdinalIgnoreCase));
        }

        public static AccountTransactionDto? BuildInitialBalanceRow(
            int accountId,
            decimal? initialBalance,
            DateTime? accountCreatedDate)
        {
            if (initialBalance is not > 0)
                return null;

            var date = accountCreatedDate ?? DateTime.UtcNow;
            return new AccountTransactionDto
            {
                TransactionId = 0,
                AccountId = accountId,
                TransactionType = 2,
                Amount = initialBalance.Value,
                BalanceAfterTransaction = initialBalance.Value,
                Reason = "Initial balance on account creation",
                ReferenceType = "AccountCreation",
                ReferenceId = accountId,
                CategoryLabel = "Opening Balance",
                InitiatedBy = 0,
                InitiatedByName = "System",
                CreatedDate = date,
                StatementDate = date,
                DetailDates = new List<StatementDateDetail>
                {
                    new()
                    {
                        Label = "Credit/Debit Date",
                        Value = date,
                        DateOnly = true
                    }
                }
            };
        }

        public static AccountTransactionDto? BuildPeriodOpeningRow(
            int accountId,
            DateTime fromDate,
            decimal openingBalance)
        {
            if (openingBalance == 0)
                return null;

            var labelDate = fromDate.Date;
            return new AccountTransactionDto
            {
                TransactionId = -1,
                AccountId = accountId,
                TransactionType = 2,
                Amount = openingBalance,
                BalanceAfterTransaction = openingBalance,
                Reason = $"Opening balance as on {labelDate:yyyy-MM-dd}",
                ReferenceType = "OpeningBalance",
                CategoryLabel = "Opening Balance",
                InitiatedBy = 0,
                InitiatedByName = "System",
                CreatedDate = labelDate,
                StatementDate = labelDate,
                DetailDates = new List<StatementDateDetail>
                {
                    new()
                    {
                        Label = "Credit/Debit Date",
                        Value = labelDate,
                        DateOnly = true
                    }
                }
            };
        }

        public static decimal ComputeOpeningFromDtos(
            IEnumerable<AccountTransactionDto> transactions,
            decimal? initialBalance,
            DateTime fromDate)
        {
            var running = initialBalance ?? 0m;
            foreach (var txn in transactions
                         .OrderBy(t => t.StatementDate)
                         .ThenBy(t => t.TransactionId))
            {
                if (txn.StatementDate.Date >= fromDate.Date)
                    break;

                if (txn.IsDebit())
                    running -= txn.Amount;
                else if (txn.IsCredit())
                    running += txn.Amount;
            }

            return running;
        }

        public static IReadOnlyList<AccountTransactionDto> AppendLedgerOpeningRows(
            IReadOnlyList<AccountTransactionDto> transactions,
            AccountBalanceDto? balance,
            DateTime? accountCreatedDate,
            DateTime? fromDate,
            IReadOnlyList<AccountTransactionDto>? allTransactionsForOpening = null)
        {
            var list = transactions.ToList();
            if (balance == null)
                return list;

            var hasInitialRow = list.Any(IsInitialBalanceTransaction);
            if (!hasInitialRow)
            {
                var initialRow = BuildInitialBalanceRow(balance.SubdealerAccountId, balance.InitialBalance, accountCreatedDate ?? balance.CreatedDate);
                if (initialRow != null)
                    list.Add(initialRow);
            }

            var openingSource = allTransactionsForOpening ?? transactions;
            {
                var opening = ComputeOpeningFromDtos(openingSource, balance.InitialBalance, fromDate.Value);
                var hasEarlierActivity = transactions.Any(t => t.StatementDate.Date < fromDate.Value.Date);
                if (hasEarlierActivity && opening != 0)
                {
                    var periodRow = BuildPeriodOpeningRow(balance.SubdealerAccountId, fromDate.Value, opening);
                    if (periodRow != null)
                        list.Add(periodRow);
                }
            }

            return list;
        }
    }
}
