using System.Net;

namespace HealthPath.API.Common;

public class ApiException(
    string message,
    ErrorCode errorCode = ErrorCode.INTERNAL_ERROR,
    HttpStatusCode statusCode = HttpStatusCode.BadRequest,
    List<string>? errors = null)
    : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public ErrorCode ErrorCode { get; } = errorCode;
    public List<string>? Errors { get; } = errors;
}
