using MediatR;

namespace CQRSMediatR.Features.Products.Commands.Delete;

public sealed record DeleteProductCommand(Guid Id) : IRequest<bool>;