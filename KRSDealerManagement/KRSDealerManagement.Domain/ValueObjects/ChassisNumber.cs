namespace KRSDealerManagement.Domain.ValueObjects
{
    /// <summary>
    /// Immutable value object representing a unique vehicle chassis number
    /// </summary>
    public class ChassisNumber : IEquatable<ChassisNumber>
    {
        /// <summary>
        /// Chassis number value
        /// </summary>
        public string Value { get; }

        public ChassisNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Chassis number cannot be empty", nameof(value));

            value = value.Trim().ToUpperInvariant();

            if (value.Length < 10 || value.Length > 20)
                throw new ArgumentException("Chassis number must be between 10 and 20 characters", nameof(value));

            // Validate format: typically alphanumeric
            if (!value.All(c => char.IsLetterOrDigit(c)))
                throw new ArgumentException("Chassis number can only contain letters and numbers", nameof(value));

            Value = value;
        }

        public override string ToString()
        {
            return Value;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ChassisNumber);
        }

        public bool Equals(ChassisNumber other)
        {
            return other != null && Value == other.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(ChassisNumber left, ChassisNumber right)
        {
            return left?.Equals(right) ?? (right == null);
        }

        public static bool operator !=(ChassisNumber left, ChassisNumber right)
        {
            return !(left == right);
        }

        public static implicit operator string(ChassisNumber chassisNumber)
        {
            return chassisNumber?.Value;
        }

        public static explicit operator ChassisNumber(string value)
        {
            return new ChassisNumber(value);
        }
    }
}
