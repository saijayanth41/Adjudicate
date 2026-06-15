using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Adjudicate.Api.Middleware;

public sealed class ExceptionMiddleware
{
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
        var (status, title) = ex switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Conflict"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = ex.Message
        };

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        return context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
