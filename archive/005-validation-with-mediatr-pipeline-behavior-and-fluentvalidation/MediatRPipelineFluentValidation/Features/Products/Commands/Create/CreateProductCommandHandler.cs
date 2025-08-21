using MediatR;
using MediatRPipelineFluentValidation.Domain;
using MediatRPipelineFluentValidation.Features.Products.DTOs;
using MediatRPipelineFluentValidation.Persistence;

namespace MediatRPipelineFluentValidation.Features.Products.Commands.Create;

public class CreateProductCommandHandler(AppDbContext context) : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        Product product = new(command.Name, command.Description, command.Price);

        await context.Products.AddAsync(product, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return new ProductDto(product.Id, product.Name, product.Description, product.Price);
    }
}