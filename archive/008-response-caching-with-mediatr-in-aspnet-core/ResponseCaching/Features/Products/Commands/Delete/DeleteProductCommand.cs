using MediatR;

namespace ResponseCaching.Features.Products.Commands.Delete;

public sealed record DeleteProductCommand(Guid Id) : IRequest<bool>;