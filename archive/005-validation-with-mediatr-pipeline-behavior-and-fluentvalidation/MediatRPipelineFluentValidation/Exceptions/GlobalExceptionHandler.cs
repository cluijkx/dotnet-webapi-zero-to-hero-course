using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MediatRPipelineFluentValidation.Exceptions;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problemDetails = new()
        {
            Instance = httpContext.Request.Path
        };

        if (exception is ValidationException fluentException)
        {
            problemDetails.Title = "one or more validation errors occurred.";
            problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

            List<string> validationErrors = [];

            foreach (ValidationFailure? error in fluentException.Errors)
            {
                validationErrors.Add(error.ErrorMessage);
            }

            problemDetails.Extensions.Add("errors", validationErrors);
        }
        else
        {
            problemDetails.Title = exception.Message;
        }

        logger.LogError("{ProblemDetailsTitle}", problemDetails.Title);

        problemDetails.Status = httpContext.Response.StatusCode;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken).ConfigureAwait(false);

        return true;
    }
}