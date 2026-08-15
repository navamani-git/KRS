namespace KRSDealerManagement.Application.DTOs
{
    /// <summary>
    /// Account Permission Data Transfer Object
    /// </summary>
    public class AccountPermissionDto
    {
        public int PermissionId { get; set; }
        public int AccountId { get; set; }
        public required string MenuKey { get; set; }
        public required string MenuName { get; set; }
        public bool IsAccessible { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanApprove { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public string GetPermissionsSummary()
        {
            if (!IsAccessible)
                return "No Access";

            var permissions = new List<string> { "View" };
            if (CanCreate) permissions.Add("Create");
            if (CanEdit) permissions.Add("Edit");
            if (CanDelete) permissions.Add("Delete");
            if (CanApprove) permissions.Add("Approve");

            return string.Join(", ", permissions);
        }
    }
}
