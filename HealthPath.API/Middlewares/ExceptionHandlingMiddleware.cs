using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using HealthPath.API.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HealthPath.API.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (ex is BadHttpRequestException)
            {
                _logger.LogWarning("A bad request exception occurred: {Message}", ex.Message);
            }
            else
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            }
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var statusCode = HttpStatusCode.InternalServerError;
        var message = "An unexpected error occurred on the server.";
        var errorCode = ErrorCode.INTERNAL_ERROR;
        List<string>? errors = null;

        switch (exception)
        {
            case ApiException apiException:
                statusCode = apiException.StatusCode;
                message = apiException.Message;
                errorCode = apiException.ErrorCode;
                errors = apiException.Errors;
                break;

            case BadHttpRequestException badRequestEx:
                statusCode = (HttpStatusCode)badRequestEx.StatusCode;
                message = badRequestEx.Message;
                errorCode = ErrorCode.VALIDATION_ERROR;
                break;

            case UnauthorizedAccessException unauthorizedEx:
                statusCode = HttpStatusCode.Unauthorized;
                message = unauthorizedEx.Message;
                errorCode = ErrorCode.UNAUTHORIZED;
                break;

            case ArgumentException argumentEx:
                statusCode = HttpStatusCode.BadRequest;
                message = argumentEx.Message;
                errorCode = ErrorCode.VALIDATION_ERROR;
                break;

            default:
                // For security reasons, do not expose detailed system exception messages in production
                #if DEBUG
                message = exception.Message;
                errors = new List<string> { exception.StackTrace ?? string.Empty };
                #endif
                break;
        }

        context.Response.StatusCode = (int)statusCode;

        var apiResponse = ApiResponse.Fail(message, errorCode, errors);
        
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(apiResponse, options);
        return context.Response.WriteAsync(json);
    }
}
