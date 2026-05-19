using System.Collections.Generic;

namespace HealthPath.API.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public string? ErrorCode { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success")
        => new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, ErrorCode errorCode, List<string>? errors = null)
        => new() { Success = false, Message = message, ErrorCode = errorCode.ToString(), Errors = errors };

    public static ApiResponse<T> Fail(string message, string errorCode, List<string>? errors = null)
        => new() { Success = false, Message = message, ErrorCode = errorCode, Errors = errors };
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Ok(string message = "Success")
        => new() { Success = true, Message = message, Data = null };

    public static new ApiResponse Fail(string message, ErrorCode errorCode, List<string>? errors = null)
        => new() { Success = false, Message = message, ErrorCode = errorCode.ToString(), Errors = errors };

    public static new ApiResponse Fail(string message, string errorCode, List<string>? errors = null)
        => new() { Success = false, Message = message, ErrorCode = errorCode, Errors = errors };
}
