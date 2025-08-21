using System.Net;

namespace GlobalExceptionHandling.Exceptions;

public class ProductNotFoundException(Guid id)
    : BaseException($"The product with id {id} is not found.", HttpStatusCode.NotFound)
{
    public Guid ProductId { get; } = id;
}