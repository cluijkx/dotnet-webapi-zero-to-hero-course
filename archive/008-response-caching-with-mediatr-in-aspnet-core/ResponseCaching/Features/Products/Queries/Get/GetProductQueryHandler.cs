using MediatR;
using ResponseCaching.Domain;
using ResponseCaching.Features.Products.Dtos;
using ResponseCaching.Persistence;

namespace ResponseCaching.Features.Products.Queries.Get;

public class GetProductQueryHandler(AppDbContext context)
    : IRequestHandler<GetProductQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductQuery query, CancellationToken cancellationToken)
    {
        Product? product = await context.Products.FindAsync([query.Id], cancellationToken: cancellationToken);

        return product is null
            ? throw new KeyNotFoundException($"Product with id '{query.Id}' was not found.")
            : new ProductDto(product.Id, product.Name, product.Description, product.Price);
    }
}