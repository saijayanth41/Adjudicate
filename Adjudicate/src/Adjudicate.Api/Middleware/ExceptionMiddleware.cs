using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Adjudicate.Api.Middleware;

public sealed class ExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteErrorAsync(context, ex);
        }
    }

    private static Task WriteErrorAsync(HttpContext context, Exception ex)
    {
        var (status, title, detail) = ex switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found", ex.Message),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Conflict", ex.Message),
            ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request", ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred.")
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        };

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        return context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
