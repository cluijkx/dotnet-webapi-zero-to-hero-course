using GlobalExceptionHandling.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace GlobalExceptionHandling.Handlers;

public class StockExhaustedExceptionHandler(ILogger<StockExhaustedExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not StockExhaustedException e)
        {
            return false;
        }

        logger.LogWarning(exception, "Stock exhausted for product: {ProductId}", e.ProductId);

        ProblemDetails problemDetails = new()
        {
            Title = "Stock exhausted",
            Detail = e.Message,
            Status = StatusCodes.Status409Conflict,
            Type = "https://httpstatuses.com/409",
            Extensions = { ["productId"] = e.ProductId }
        };

        httpContext.Response.StatusCode = (int)HttpStatusCode.Conflict;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}