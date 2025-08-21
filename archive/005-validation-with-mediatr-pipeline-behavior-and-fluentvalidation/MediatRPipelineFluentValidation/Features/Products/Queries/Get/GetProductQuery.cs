using MediatRPipelineFluentValidation.Features.Products.DTOs;
using MediatR;

namespace MediatRPipelineFluentValidation.Features.Products.Queries.Get;

public sealed record GetProductQuery(Guid Id) : IRequest<ProductDto>;