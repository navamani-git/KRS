using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Shared.Helpers;

namespace KRSDealerManagement.Application.Helpers
{
    public static class AuditUserHelper
    {
        public static string ResolveName(int? userId, IReadOnlyDictionary<int, User> users)
        {
            if (!userId.HasValue || userId.Value <= 0)
                return "—";

            return users.TryGetValue(userId.Value, out var user)
                ? user.GetFullName()
                : $"User #{userId.Value}";
        }
    }
}
