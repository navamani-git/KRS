namespace KRSDealerManagement.Shared.Exceptions
{
    /// <summary>
    /// Exception thrown when user lacks required permissions or authorization
    /// </summary>
    public class UnauthorizedAccessException : Exception
    {
        private readonly string _requiredPermission;

        public string RequiredPermission => _requiredPermission;

        public UnauthorizedAccessException(string message) 
            : base(message)
        {
            _requiredPermission = string.Empty;
        }

        public UnauthorizedAccessException(string message, string requiredPermission) 
            : base(message)
        {
            _requiredPermission = requiredPermission;
        }

        public static UnauthorizedAccessException ForPermission(string permission)
        {
            return new UnauthorizedAccessException(
                $"You do not have permission to access: {permission}", 
                permission);
        }

        public static UnauthorizedAccessException ForAccount(int accountId)
        {
            return new UnauthorizedAccessException(
                $"You do not have access to account {accountId}");
        }
    }
}
