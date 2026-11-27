
using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using ControlInventario.Core.CustomEntities;
using ControlInventario.Core.Exceptions;

namespace ControlInventario.Api.Filters
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Excepcion capturada por el Middleware Global.");

                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(
            HttpContext context,
            Exception exception)
        {
            var code = HttpStatusCode.InternalServerError;

            string message = "Error interno no controlado.";

            object? errors = null;

            switch (exception)
            {
                case BusinessException bizEx:
                    // Convertir el codigo entero a HttpStatusCode.
                    code = (HttpStatusCode)bizEx.StatusCode;
                    message = bizEx.Message;
                    errors = bizEx.Details;
                    break;

                case ValidationException valEx:
                    code = HttpStatusCode.BadRequest;
                    message = "Errores de validacion en los datos enviados.";

                    errors = valEx.Errors.Select(e => new
                    {
                        Field = e.PropertyName,
                        Error = e.ErrorMessage
                    });

                    break;
            }

            var response = new ErrorResponse
            {
                Status = (int)code,
                Title = code.ToString(),
                Message = message,
                Errors = errors,
                TraceId = context.TraceIdentifier
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)code;

            var opciones = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return context.Response.WriteAsync(
                JsonSerializer.Serialize(response, opciones));
        }
    }
}
