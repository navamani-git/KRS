namespace KRSDealerManagement.Domain.Entities
{
    /// <summary>Dynamic role master (SYSTEM_ADMIN, BRANCH_MANAGER, FINANCE_ADMIN, SUBDEALER, …).</summary>
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleCode { get; set; } = "";
        public string RoleName { get; set; } = "";
        public string? Description { get; set; }
        public string? RoleTemplateCode { get; set; }
        public int? DealershipId { get; set; }
        public bool IsSystemRole { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>KRS dealership location (Karur, Namakkal, …). Add rows to support new locations.</summary>
    public class Dealership
    {
        public int DealershipId { get; set; }
        public string DealershipCode { get; set; } = "";
        public string DealershipName { get; set; } = "";
        public string? Location { get; set; }
        public string? ContactPhone { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Business subdealer under a dealership location (not the login row).</summary>
    public class SubDealer
    {
        public int SubDealerId { get; set; }
        public int DealershipId { get; set; }
        public string? SubDealerCode { get; set; }
        public string SubDealerName { get; set; } = "";
        public string? Location { get; set; }
        public string? PrimaryPhone { get; set; }
        public string? SecondaryPhone { get; set; }
        public string? SalesRepMobile { get; set; }
        public string? ServiceRepMobile { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Menus granted to a role (fetched from DB — not hardcoded enums).</summary>
    public class RoleMenu
    {
        public int RoleMenuId { get; set; }
        public int RoleId { get; set; }
        public string MenuKey { get; set; } = "";
        public string MenuName { get; set; } = "";
        public bool IsAccessible { get; set; } = true;
        public bool IsReadOnly { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Maps a login user to a role within the hierarchy.
    /// Branch Manager / Finance Admin → DealershipId set, SubDealerId null.
    /// Subdealer → DealershipId + SubDealerId set.
    /// System Admin → both null.
    /// </summary>
    public class UserOrgRole
    {
        public int UserOrgRoleId { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public int? DealershipId { get; set; }
        public int? SubDealerId { get; set; }
        public bool IsPrimary { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    }
}
