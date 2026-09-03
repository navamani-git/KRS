using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace KRSDealerManagement.Web.Helpers
{
    public static class QuickActionsHelper
    {
        public static async Task<QuickActionsWidgetModel?> BuildForSessionAsync(
            ISession session,
            IUrlHelper url,
            IUnitOfWork unitOfWork)
        {
            if (!SessionHelper.IsAuthenticated(session))
                return null;

            var userId = SessionHelper.GetUserId(session);
            if (!userId.HasValue)
                return null;

            string UrlFor(string controller, string action, IReadOnlyDictionary<string, object>? routeValues) =>
                routeValues is { Count: > 0 }
                    ? url.Action(action, controller, routeValues) ?? "#"
                    : url.Action(action, controller) ?? "#";

            var catalog = DashboardQuickActions.GetCatalog(session, UrlFor);

            var savedKeys = SessionHelper.GetQuickActionKeys(session);
            if (savedKeys == null)
            {
                var user = await unitOfWork.Users.GetByIdAsync(userId.Value);
                savedKeys = user?.QuickActionKeys;
                if (savedKeys != null)
                    session.SetString("QuickActionKeys", savedKeys);
            }

            return new QuickActionsWidgetModel
            {
                QuickActions = DashboardQuickActions.Resolve(catalog, savedKeys),
                Catalog = catalog,
                SelectedKeys = DashboardQuickActions.ParseSavedKeys(savedKeys, catalog)
            };
        }
    }
}
