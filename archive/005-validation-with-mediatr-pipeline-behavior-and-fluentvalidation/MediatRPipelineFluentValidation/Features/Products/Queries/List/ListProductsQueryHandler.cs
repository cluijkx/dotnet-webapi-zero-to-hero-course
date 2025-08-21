using MediatRPipelineFluentValidation.Features.Products.DTOs;
using MediatRPipelineFluentValidation.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MediatRPipelineFluentValidation.Features.Products.Queries.List;

public class ListProductsQueryHandler(AppDbContext context) : IRequestHandler<ListProductsQuery, List<ProductDto>>
{
    public async Task<List<ProductDto>> Handle(ListProductsQuery query, CancellationToken cancellationToken)
    {
        return await context.Products
            .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price))
            .ToListAsync(cancellationToken: cancellationToken);
    }
}