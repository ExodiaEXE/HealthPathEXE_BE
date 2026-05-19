using System;
using System.Collections.Generic;
using System.Net;

namespace HealthPath.API.Common;

public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public ErrorCode ErrorCode { get; }
    public List<string>? Errors { get; }

    public ApiException(
        string message, 
        ErrorCode errorCode = ErrorCode.INTERNAL_ERROR, 
        HttpStatusCode statusCode = HttpStatusCode.BadRequest, 
        List<string>? errors = null) 
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
        Errors = errors;
    }
}
