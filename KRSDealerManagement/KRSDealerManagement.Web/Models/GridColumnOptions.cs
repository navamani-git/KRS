using KRSDealerManagement.Web.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace KRSDealerManagement.Web.Models
{
    public sealed class GridColumnOptions
    {
        public bool HideLogins { get; init; }
        public bool HideActions { get; init; }
        public bool IsSubdealer { get; init; }
        public bool ShowBranchColumn { get; init; }
        public bool IsSubdealerView { get; init; }

        public static GridColumnOptions FromViewBag(ViewDataDictionary viewBag, HttpContext httpContext)
        {
            var session = httpContext.Session;
            var isBranchManager = viewBag["IsBranchManager"] as bool? ?? SessionHelper.IsBranchManager(session);
            return new GridColumnOptions
            {
                HideLogins = viewBag["HideLogins"] as bool? ?? isBranchManager,
                HideActions = viewBag["HideActions"] as bool? ?? isBranchManager,
                IsSubdealer = viewBag["IsSubdealer"] as bool? ?? SessionHelper.IsSubdealer(session),
                ShowBranchColumn = viewBag["ShowBranchColumn"] as bool?
                    ?? viewBag["ShowDealershipColumn"] as bool?
                    ?? false,
                IsSubdealerView = viewBag["IsSubdealerView"] as bool? ?? false
            };
        }
    }
}
