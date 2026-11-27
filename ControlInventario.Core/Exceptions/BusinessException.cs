using System;
using System.Net;

namespace ControlInventario.Core.Exceptions
{
    public class BusinessException : Exception
    {
        public int StatusCode { get; }

        public object? Details { get; }

        public BusinessException(
            string message,
            int statusCode = (int)HttpStatusCode.BadRequest,
            object? details = null)
            : base(message)
        {
            StatusCode = statusCode;
            Details = details;
        }

        public BusinessException(
            string message,
            HttpStatusCode statusCode,
            object? details = null)
            : this(message, (int)statusCode, details)
        {
        }
    }
}
