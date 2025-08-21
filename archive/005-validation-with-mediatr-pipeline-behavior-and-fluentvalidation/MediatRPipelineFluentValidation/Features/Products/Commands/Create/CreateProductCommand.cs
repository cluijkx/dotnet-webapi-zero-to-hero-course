using MediatR;
using MediatRPipelineFluentValidation.Features.Products.DTOs;

namespace MediatRPipelineFluentValidation.Features.Products.Commands.Create;

public sealed record CreateProductCommand(string Name, string Description, decimal Price) : IRequest<ProductDto>;