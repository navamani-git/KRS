using KRSDealerManagement.Shared.Enums;

namespace KRSDealerManagement.Application.Services
{
    public class MenuAccessEntry
    {
        public string MenuKey { get; set; } = "";
        public MenuAccessLevel Level { get; set; }
    }
}
