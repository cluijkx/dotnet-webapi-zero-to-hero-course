using MediatRPipelineFluentValidation.Features.Products.DTOs;
using MediatR;

namespace MediatRPipelineFluentValidation.Features.Products.Queries.List;

public sealed record ListProductsQuery : IRequest<List<ProductDto>>;