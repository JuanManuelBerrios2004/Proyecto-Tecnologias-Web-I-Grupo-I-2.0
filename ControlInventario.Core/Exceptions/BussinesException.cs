using System.Net;

namespace ControlInventario.Core.Exceptions
{
    /// Error de regla de negocio. Lo lanzan los servicios y lo traduce a una respuesta HTTP

    public class BusinessException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public object? Details { get; }

        public BusinessException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest, object? details = null)
            : base(message)
        {
            StatusCode = statusCode;
            Details = details;
        }
    }
}