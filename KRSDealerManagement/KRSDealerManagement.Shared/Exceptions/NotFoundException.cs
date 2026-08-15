namespace KRSDealerManagement.Shared.Exceptions
{
    /// <summary>
    /// Exception thrown when a requested entity is not found
    /// </summary>
    public class NotFoundException : Exception
    {
        private readonly string _name;
        private readonly object _key;

        public string Name => _name;
        public object Key => _key;

        public NotFoundException(string name, object key)
            : base($"Entity \"{name}\" ({key}) was not found.")
        {
            _name = name ?? string.Empty;
            _key = key ?? new object();
        }

        public NotFoundException(string message) : base(message)
        {
            _name = string.Empty;
            _key = new object();
        }
    }
}
