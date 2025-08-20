using CQRSMediatR.Persistence;
using MediatR;

namespace CQRSMediatR.Features.Products.Commands.Delete;

public class DeleteProductCommandHandler(AppDbContext context) : IRequestHandler<DeleteProductCommand, bool>
{
    public async Task<bool> Handle(DeleteProductCommand command, CancellationToken cancellationToken)
    {
        Domain.Product? product = await context.Products.FindAsync([command.Id], cancellationToken: cancellationToken);

        if (product is null)
        {
            return false;
        }

        context.Products.Remove(product);

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}