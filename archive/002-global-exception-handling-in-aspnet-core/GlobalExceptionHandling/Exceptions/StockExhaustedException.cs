using System.Net;

namespace GlobalExceptionHandling.Exceptions;

public class StockExhaustedException(Guid id)
    : BaseException($"The product with ID '{id}' is out of stock.", HttpStatusCode.NotFound)
{
    public Guid ProductId { get; } = id;
}