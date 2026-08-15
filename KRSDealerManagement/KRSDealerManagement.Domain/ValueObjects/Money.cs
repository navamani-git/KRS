namespace KRSDealerManagement.Domain.ValueObjects
{
    /// <summary>
    /// Immutable value object representing a monetary amount
    /// Encapsulates currency and amount validation
    /// </summary>
    public class Money : IEquatable<Money>
    {
        /// <summary>
        /// Amount in rupees
        /// </summary>
        public decimal Amount { get; }

        /// <summary>
        /// Currency symbol (default: ₹ for Indian Rupee)
        /// </summary>
        public string Currency { get; }

        public Money(decimal amount, string currency = "₹")
        {
            if (amount < 0)
                throw new ArgumentException("Amount cannot be negative", nameof(amount));

            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("Currency cannot be empty", nameof(currency));

            Amount = amount;
            Currency = currency;
        }

        /// <summary>
        /// Add two money values
        /// </summary>
        public Money Add(Money other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (Currency != other.Currency)
                throw new InvalidOperationException($"Cannot add different currencies: {Currency} and {other.Currency}");

            return new Money(Amount + other.Amount, Currency);
        }

        /// <summary>
        /// Subtract two money values
        /// </summary>
        public Money Subtract(Money other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (Currency != other.Currency)
                throw new InvalidOperationException($"Cannot subtract different currencies: {Currency} and {other.Currency}");

            return new Money(Amount - other.Amount, Currency);
        }

        /// <summary>
        /// Multiply amount by a factor
        /// </summary>
        public Money Multiply(decimal factor)
        {
            return new Money(Amount * factor, Currency);
        }

        /// <summary>
        /// Divide amount by a factor
        /// </summary>
        public Money Divide(decimal factor)
        {
            if (factor == 0)
                throw new ArgumentException("Cannot divide by zero", nameof(factor));

            return new Money(Amount / factor, Currency);
        }

        /// <summary>
        /// Check if amount is zero
        /// </summary>
        public bool IsZero => Amount == 0;

        /// <summary>
        /// Check if amount is positive
        /// </summary>
        public bool IsPositive => Amount > 0;

        /// <summary>
        /// Check if amount is less than other
        /// </summary>
        public bool IsLessThan(Money other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (Currency != other.Currency)
                throw new InvalidOperationException($"Cannot compare different currencies");

            return Amount < other.Amount;
        }

        /// <summary>
        /// Check if amount is greater than other
        /// </summary>
        public bool IsGreaterThan(Money other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (Currency != other.Currency)
                throw new InvalidOperationException($"Cannot compare different currencies");

            return Amount > other.Amount;
        }

        /// <summary>
        /// Format as currency string
        /// </summary>
        public override string ToString()
        {
            return $"{Currency}{Amount:N2}";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Money);
        }

        public bool Equals(Money other)
        {
            return other != null &&
                   Amount == other.Amount &&
                   Currency == other.Currency;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Amount, Currency);
        }

        public static bool operator ==(Money left, Money right)
        {
            return left?.Equals(right) ?? (right == null);
        }

        public static bool operator !=(Money left, Money right)
        {
            return !(left == right);
        }

        public static Money operator +(Money left, Money right)
        {
            return left?.Add(right) ?? throw new ArgumentNullException(nameof(left));
        }

        public static Money operator -(Money left, Money right)
        {
            return left?.Subtract(right) ?? throw new ArgumentNullException(nameof(left));
        }

        public static Money operator *(Money left, decimal right)
        {
            return left?.Multiply(right) ?? throw new ArgumentNullException(nameof(left));
        }

        public static Money operator /(Money left, decimal right)
        {
            return left?.Divide(right) ?? throw new ArgumentNullException(nameof(left));
        }

        public static bool operator <(Money left, Money right)
        {
            return left?.IsLessThan(right) ?? throw new ArgumentNullException(nameof(left));
        }

        public static bool operator >(Money left, Money right)
        {
            return left?.IsGreaterThan(right) ?? throw new ArgumentNullException(nameof(left));
        }

        public static bool operator <=(Money left, Money right)
        {
            return left?.IsLessThan(right) != false && left != right;
        }

        public static bool operator >=(Money left, Money right)
        {
            return left?.IsGreaterThan(right) != false && left != right;
        }
    }
}
