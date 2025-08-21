using System.Net;

namespace GlobalExceptionHandling.Exceptions;

public class BaseException(string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
    : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}