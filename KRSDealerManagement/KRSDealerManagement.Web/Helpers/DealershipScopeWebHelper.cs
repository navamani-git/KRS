using KRSDealerManagement.Web.Helpers;

namespace KRSDealerManagement.Web.Helpers
{
    public static class DealershipScopeWebHelper
    {
        public static void ApplyStaffScope<T>(ISession session, T query)
            where T : class
        {
            var ids = SessionHelper.GetEffectiveDealershipIds(session);
            var legacy = SessionHelper.GetDealershipScope(session);

            var type = typeof(T);
            type.GetProperty("DealershipIds")?.SetValue(query, ids);
            type.GetProperty("DealershipId")?.SetValue(query, legacy);
        }
    }
}
