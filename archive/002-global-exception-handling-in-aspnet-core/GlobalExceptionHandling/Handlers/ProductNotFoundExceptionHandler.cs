using GlobalExceptionHandling.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace GlobalExceptionHandling.Handlers;

public class ProductNotFoundExceptionHandler(ILogger<ProductNotFoundExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ProductNotFoundException e)
        {
            return false;
        }

        logger.LogWarning(exception, "Product not found: {ProductId}", e.ProductId);

        ProblemDetails problemDetails = new()
        {
            Title = "Product not found",
            Detail = e.Message,
            Status = StatusCodes.Status404NotFound,
            Type = "https://httpstatuses.com/404",
            Extensions = { ["productId"] = e.ProductId }
        };

        httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}