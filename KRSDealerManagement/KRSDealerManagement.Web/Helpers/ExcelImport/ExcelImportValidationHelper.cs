using KRSDealerManagement.Web.Models;

namespace KRSDealerManagement.Web.Helpers.ExcelImport
{
    public static class ExcelImportValidationHelper
    {
        public static string? Require(ExcelImportRow row, string column, ICollection<ExcelImportError> errors)
        {
            var val = row.Get(column)?.Trim();
            if (string.IsNullOrWhiteSpace(val))
            {
                errors.Add(new ExcelImportError
                {
                    RowNumber = row.RowNumber,
                    Column = column,
                    Message = $"{column} is required."
                });
                return null;
            }
            return val;
        }

        public static bool TryInt(ExcelImportRow row, string column, ICollection<ExcelImportError> errors, out int value)
        {
            value = 0;
            var raw = Require(row, column, errors);
            if (raw == null) return false;
            if (!int.TryParse(raw, out value))
            {
                errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = column, Message = $"{column} must be a whole number." });
                return false;
            }
            return true;
        }

        public static bool TryDecimal(ExcelImportRow row, string column, ICollection<ExcelImportError> errors, out decimal value)
        {
            value = 0;
            var raw = Require(row, column, errors);
            if (raw == null) return false;
            if (!decimal.TryParse(raw, out value))
            {
                errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = column, Message = $"{column} must be a number." });
                return false;
            }
            return true;
        }

        public static bool TryDate(ExcelImportRow row, string column, ICollection<ExcelImportError> errors, out DateTime value)
        {
            value = default;
            var raw = Require(row, column, errors);
            if (raw == null) return false;
            if (!DateTime.TryParse(raw, out value))
            {
                errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = column, Message = $"{column} must be a valid date (yyyy-MM-dd)." });
                return false;
            }
            value = value.Date;
            return true;
        }

        public static bool TryYesNo(ExcelImportRow row, string column, ICollection<ExcelImportError> errors, out bool value)
        {
            value = false;
            var raw = Require(row, column, errors);
            if (raw == null) return false;
            if (raw.Equals("Yes", StringComparison.OrdinalIgnoreCase) || raw.Equals("Y", StringComparison.OrdinalIgnoreCase) || raw == "1")
            {
                value = true;
                return true;
            }
            if (raw.Equals("No", StringComparison.OrdinalIgnoreCase) || raw.Equals("N", StringComparison.OrdinalIgnoreCase) || raw == "0")
            {
                value = false;
                return true;
            }
            errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = column, Message = $"{column} must be Yes or No." });
            return false;
        }

        public static void DuplicateInFile(ExcelImportRow row, string key, HashSet<string> seen, string label, ICollection<ExcelImportError> errors)
        {
            if (!seen.Add(key))
                errors.Add(new ExcelImportError { RowNumber = row.RowNumber, Column = label, Message = $"Duplicate {label} '{key}' in file." });
        }
    }
}
