using MediatR;
using MediatRPipelineFluentValidation.Domain;
using MediatRPipelineFluentValidation.Features.Products.DTOs;
using MediatRPipelineFluentValidation.Persistence;

namespace MediatRPipelineFluentValidation.Features.Products.Commands.Update
{
    public class UpdateProductCommandHandler(AppDbContext context) : IRequestHandler<UpdateProductWithIdCommand, ProductDto?>
    {
        public async Task<ProductDto?> Handle(UpdateProductWithIdCommand command, CancellationToken cancellationToken)
        {
            Product? product = await context.Products.FindAsync([command.Id], cancellationToken);

            if (product is null)
            {
                return null;
            }

            product.Update(command.Name, command.Description, command.Price);

            await context.SaveChangesAsync(cancellationToken);

            return new ProductDto(product.Id, product.Name, product.Description, product.Price);
        }
    }
}