namespace KRSDealerManagement.Web.Helpers
{
    public class ListPageInfo
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = ListPagingHelper.DefaultPageSize;
        public int TotalItems { get; set; }
        public int TotalPages => PageSize <= 0
            ? 1
            : Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }

    public static class ListPagingHelper
    {
        public const int DefaultPageSize = 50;

        public static readonly int[] AllowedPageSizes = { 10, 25, 50, 100, 250, 500, 1000 };

        public static int ResolvePageSize(int? pageSize)
        {
            if (pageSize is > 0 && AllowedPageSizes.Contains(pageSize.Value))
                return pageSize.Value;
            return DefaultPageSize;
        }

        public static (List<T> Items, ListPageInfo Info) Paginate<T>(
            IEnumerable<T> source,
            int? page,
            int? pageSize = null)
        {
            var size = ResolvePageSize(pageSize);
            var list = source as IList<T> ?? source.ToList();
            var total = list.Count;
            var info = new ListPageInfo
            {
                PageSize = size,
                TotalItems = total,
                Page = 1
            };

            var p = page is > 0 ? page.Value : 1;
            if (p > info.TotalPages) p = info.TotalPages;
            info.Page = p;

            var items = list.Skip((p - 1) * size).Take(size).ToList();
            return (items, info);
        }

        public static void ApplyToViewBag(dynamic viewBag, ListPageInfo info)
        {
            viewBag.Page = info.Page;
            viewBag.PageSize = info.PageSize;
            viewBag.TotalItems = info.TotalItems;
            viewBag.TotalPages = info.TotalPages;
        }

        /// <summary>
        /// Default search range: first day of the month three months ago → last day of current month (inclusive).
        /// </summary>
        public static (DateTime FromDate, DateTime ToDate) GetDefaultSearchDateRange(DateTime? referenceDate = null)
        {
            var today = (referenceDate ?? DateTime.Today).Date;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var from = monthStart.AddMonths(-3);
            var to = monthStart.AddMonths(1).AddDays(-1);
            return (from, to);
        }

        /// <summary>
        /// Applies <see cref="GetDefaultSearchDateRange"/> when from/to are omitted.
        /// </summary>
        public static (DateTime FromDate, DateTime ToDate) ResolveDateRange(
            DateTime? fromDate,
            DateTime? toDate)
        {
            var (defaultFrom, defaultTo) = GetDefaultSearchDateRange();
            var from = (fromDate ?? defaultFrom).Date;
            var to = (toDate ?? defaultTo).Date;
            if (to < from) to = from;
            return (from, to);
        }
    }
}
