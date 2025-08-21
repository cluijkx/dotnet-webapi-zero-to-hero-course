using MediatR;

namespace MediatRPipelineFluentValidation.Features.Products.Commands.Delete;

public sealed record DeleteProductCommand(Guid Id) : IRequest<bool>;