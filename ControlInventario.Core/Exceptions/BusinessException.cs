using System;

namespace ControlInventario.Core.Exceptions
{
    public class BusinessException : Exception
    {
        public int StatusCode { get; }

        public object? Details { get; }

        public BusinessException(
            string message,
            int statusCode = 400,
            object? details = null)
            : base(message)
        {
            StatusCode = statusCode;
            Details = details;
        }
    }
}