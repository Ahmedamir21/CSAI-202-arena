namespace SwiftShip.Web.Models
{
    /// <summary>
    /// What a service answers with. The controllers turn a failed result into a validation
    /// message on the same page instead of an exception, so the user always sees something
    /// useful in English.
    /// </summary>
    public class Result
    {
        public bool Success { get; set; }
        public string Error { get; set; }

        public static Result Ok()
        {
            return new Result { Success = true };
        }

        public static Result Fail(string error)
        {
            return new Result { Success = false, Error = error };
        }
    }

    public class Result<T> : Result
    {
        public T Value { get; set; }

        public static Result<T> Ok(T value)
        {
            return new Result<T> { Success = true, Value = value };
        }

        public static new Result<T> Fail(string error)
        {
            return new Result<T> { Success = false, Error = error };
        }
    }
}
