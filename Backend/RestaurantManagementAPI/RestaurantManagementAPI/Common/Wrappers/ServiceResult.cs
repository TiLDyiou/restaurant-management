namespace RestaurantManagementAPI.Common.Wrappers
{
    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ServiceResult<T> Ok(T data, string message = "Thành công")
            => new ServiceResult<T> { Success = true, Data = data, Message = message };

        public static ServiceResult<T> Fail(string message)
            => new ServiceResult<T> { Success = false, Data = default, Message = message };
    }

    public class ServiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public static ServiceResult Ok(string message = "Thành công")
            => new ServiceResult { Success = true, Message = message };
        public static ServiceResult Fail(string message)
            => new ServiceResult { Success = false, Message = message };
    }
}