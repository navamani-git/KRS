using Microsoft.AspNetCore.Http;

namespace KRSDealerManagement.Web.Helpers
{
    /// <summary>
    /// Resolves subdealer org id for data scope (vehicles, bookings, orders).
    /// Login UserId remains in session for audit/actions; SubDealerId is the data key.
    /// </summary>
    public static class SubdealerScopeWebHelper
    {
        public static int? GetOrgId(ISession session) => SessionHelper.GetSubDealerId(session);

        public static int? GetOrgIdForFilter(ISession session, int? staffFilterSubdealerId)
        {
            if (SessionHelper.IsSubdealer(session))
                return GetOrgId(session);

            return staffFilterSubdealerId;
        }
    }
}
