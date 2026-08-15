namespace KRSDealerManagement.Shared.Results
{
    /// <summary>
    /// Generic result wrapper for operation responses
    /// Allows returning success/failure with data and error messages
    /// </summary>
    public class Result<T>
    {
        public bool Succeeded { get; set; }
        public required string Message { get; set; }
        public List<string> Errors { get; set; }
        public T? Data { get; set; }

        public Result()
        {
            Errors = new List<string>();
        }

        public static Result<T> Success(T data, string message = "Operation completed successfully")
        {
            return new Result<T>
            {
                Succeeded = true,
                Data = data,
                Message = message,
                Errors = new List<string>()
            };
        }

        public static Result<T> Failure(string error, string message = "Operation failed")
        {
            return new Result<T>
            {
                Succeeded = false,
                Message = message,
                Errors = new List<string> { error }
            };
        }

        public static Result<T> Failure(List<string> errors, string message = "Operation failed")
        {
            return new Result<T>
            {
                Succeeded = false,
                Message = message,
                Errors = errors
            };
        }

        public static Result<T> Failure(Dictionary<string, string[]> errors, string message = "Validation failed")
        {
            var errorList = errors
                .SelectMany(x => x.Value.Select(v => $"{x.Key}: {v}"))
                .ToList();

            return new Result<T>
            {
                Succeeded = false,
                Message = message,
                Errors = errorList
            };
        }
    }

    /// <summary>
    /// Non-generic result wrapper for operations without return data
    /// </summary>
    public class Result
    {
        public bool Succeeded { get; set; }
        public required string Message { get; set; }
        public List<string> Errors { get; set; }

        public Result()
        {
            Errors = new List<string>();
        }

        public static Result Success(string message = "Operation completed successfully")
        {
            return new Result
            {
                Succeeded = true,
                Message = message,
                Errors = new List<string>()
            };
        }

        public static Result Failure(string error, string message = "Operation failed")
        {
            return new Result
            {
                Succeeded = false,
                Message = message,
                Errors = new List<string> { error }
            };
        }

        public static Result Failure(List<string> errors, string message = "Operation failed")
        {
            return new Result
            {
                Succeeded = false,
                Message = message,
                Errors = errors
            };
        }

        public static Result Failure(Dictionary<string, string[]> errors, string message = "Validation failed")
        {
            var errorList = errors
                .SelectMany(x => x.Value.Select(v => $"{x.Key}: {v}"))
                .ToList();

            return new Result
            {
                Succeeded = false,
                Message = message,
                Errors = errorList
            };
        }
    }
}
