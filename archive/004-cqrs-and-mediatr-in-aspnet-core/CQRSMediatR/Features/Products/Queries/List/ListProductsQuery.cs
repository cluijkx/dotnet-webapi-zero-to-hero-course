using CQRSMediatR.Features.Products.DTOs;
using MediatR;

namespace CQRSMediatR.Features.Products.Queries.List;

public sealed record ListProductsQuery : IRequest<List<ProductDto>>;