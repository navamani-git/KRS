namespace KRSDealerManagement.Domain.Entities
{
    public class RoleTemplate
    {
        public int RoleTemplateId { get; set; }
        public string TemplateCode { get; set; } = "";
        public string TemplateName { get; set; } = "";
        public string? Description { get; set; }
        public int LegacyUserRole { get; set; } = 4;
        public bool IsActive { get; set; } = true;
        public int? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    }

    public class RoleTemplateMenu
    {
        public int RoleTemplateMenuId { get; set; }
        public int RoleTemplateId { get; set; }
        public string MenuKey { get; set; } = "";
        public bool IsReadOnly { get; set; }
        public int SortOrder { get; set; }
    }
}
