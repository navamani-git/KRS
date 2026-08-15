namespace KRSDealerManagement.Shared.Exceptions
{
    /// <summary>
    /// Exception thrown when validation fails
    /// Contains validation errors with field names
    /// </summary>
    public class ValidationException : Exception
    {
        private readonly Dictionary<string, string[]> _errors;

        public Dictionary<string, string[]> Errors => _errors;

        public ValidationException(string message) 
            : base(message)
        {
            _errors = new Dictionary<string, string[]>();
        }

        public ValidationException(Dictionary<string, string[]> errors) 
            : base("One or more validation failures have occurred.")
        {
            _errors = errors;
        }

        public ValidationException(string fieldName, string message) 
            : base(message)
        {
            _errors = new Dictionary<string, string[]>
            {
                { fieldName, new[] { message } }
            };
        }
    }
}
