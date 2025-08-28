using MediatR;
using ResponseCaching.Features.Products.Dtos;

namespace ResponseCaching.Features.Products.Commands.Create;

public sealed record CreateProductCommand(string Name, string Description, decimal Price) : IRequest<ProductDto>;