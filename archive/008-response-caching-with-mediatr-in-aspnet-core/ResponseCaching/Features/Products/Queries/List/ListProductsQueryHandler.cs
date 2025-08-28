using MediatR;
using Microsoft.EntityFrameworkCore;
using ResponseCaching.Features.Products.Dtos;
using ResponseCaching.Persistence;

namespace ResponseCaching.Features.Products.Queries.List;

public class ListProductsQueryHandler(AppDbContext context) : IRequestHandler<ListProductsQuery, List<ProductDto>>
{
    public async Task<List<ProductDto>> Handle(ListProductsQuery query, CancellationToken cancellationToken)
    {
        return await context.Products
            .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price))
            .ToListAsync(cancellationToken: cancellationToken);
    }
}