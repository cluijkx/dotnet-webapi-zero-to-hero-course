using CQRSMediatR.Features.Products.DTOs;
using MediatR;

namespace CQRSMediatR.Features.Products.Commands.Create;

public sealed record CreateProductCommand(string Name, string Description, decimal Price) : IRequest<ProductDto>;