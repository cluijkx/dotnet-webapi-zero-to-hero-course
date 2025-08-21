using GlobalExceptionHandling.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace GlobalExceptionHandling;

public class ErrorHandlerMiddleware(RequestDelegate _next, ILogger<ErrorHandlerMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception error)
        {
            HttpResponse response = context.Response;
            response.ContentType = "application/json";
            response.StatusCode = error switch
            {
                BaseException e => (int)e.StatusCode,
                _ => StatusCodes.Status500InternalServerError
            };

            ProblemDetails problemDetails = new()
            {
                Status = response.StatusCode,
                Title = error.Message,
                Instance = context.Request.Path
            };

            logger.LogError(error, "Unhandled exception of type {ExceptionType} at {Path}",
                error.GetType().Name, context.Request.Path);

            string result = JsonSerializer.Serialize(problemDetails);

            await response.WriteAsync(result);
        }
    }
}