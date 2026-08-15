namespace KRSDealerManagement.Shared.Results
{
    /// <summary>
    /// Generic result wrapper for paginated data
    /// Includes pagination metadata (total items, page size, current page)
    /// </summary>
    public class PagedResult<T>
    {
        public bool Succeeded { get; set; }
        public required string Message { get; set; }
        public List<string> Errors { get; set; }
        public required List<T> Data { get; set; }

        // Pagination metadata
        public int TotalItems { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public PagedResult()
        {
            Data = new List<T>();
            Errors = new List<string>();
        }

        public static PagedResult<T> Success(
            List<T> data,
            int totalItems,
            int pageNumber,
            int pageSize,
            string message = "Operation completed successfully")
        {
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return new PagedResult<T>
            {
                Succeeded = true,
                Data = data,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                Message = message,
                Errors = new List<string>()
            };
        }

        public static PagedResult<T> Failure(string error, string message = "Operation failed")
        {
            return new PagedResult<T>
            {
                Succeeded = false,
                Message = message,
                Errors = new List<string> { error },
                Data = new List<T>(),
                TotalItems = 0,
                TotalPages = 0
            };
        }

        public static PagedResult<T> Failure(List<string> errors, string message = "Operation failed")
        {
            return new PagedResult<T>
            {
                Succeeded = false,
                Message = message,
                Errors = errors,
                Data = new List<T>(),
                TotalItems = 0,
                TotalPages = 0
            };
        }
    }
}
