using MediatR;
using System.Text.Json;

namespace MediatRPipelineFluentValidation.Behaviors;

public class RequestResponseLoggingBehavior<TRequest, TResponse>(ILogger<RequestResponseLoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Request Logging
        Guid correlationId = Guid.NewGuid();

        string requestJson = JsonSerializer.Serialize(request);

        logger.LogInformation("Handling request {CorrelationID}: {Request}", correlationId, requestJson);

        // Response logging
        TResponse? response = await next();

        string responseJson = JsonSerializer.Serialize(response);

        logger.LogInformation("Response for {CorrelationID}: {Response}", correlationId, responseJson);

        // Return response
        return response;
    }
}