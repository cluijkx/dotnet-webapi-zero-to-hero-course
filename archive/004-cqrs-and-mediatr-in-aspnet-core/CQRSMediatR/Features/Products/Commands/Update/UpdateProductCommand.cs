using CQRSMediatR.Features.Products.DTOs;
using MediatR;

namespace CQRSMediatR.Features.Products.Commands.Update;

public record UpdateProductCommand(string Name, string Description, decimal Price) : IRequest<ProductDto>;

public sealed record UpdateProductWithIdCommand(Guid Id, string Name, string Description, decimal Price)
    : UpdateProductCommand(Name, Description, Price), IRequest<ProductDto>
{
    public UpdateProductWithIdCommand(Guid id, UpdateProductCommand command)
        : this(id, command.Name, command.Description, command.Price)
    {

    }
}