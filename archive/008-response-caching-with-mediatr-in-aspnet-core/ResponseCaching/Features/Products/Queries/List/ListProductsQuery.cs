using MediatR;
using ResponseCaching.Features.Products.Dtos;

namespace ResponseCaching.Features.Products.Queries.List;

public sealed record ListProductsQuery : IRequest<List<ProductDto>>;