using ControlInventario.Core.Exceptions;

namespace ControlInventario.Api.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(
            RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (BusinessException ex)
            {
                context.Response.StatusCode =
                    ex.StatusCode;

                context.Response.ContentType =
                    "application/json";

                await context.Response.WriteAsJsonAsync(
                    new
                    {
                        message = ex.Message,
                        details = ex.Details
                    });
            }
            catch (Exception)
            {
                context.Response.StatusCode = 500;

                context.Response.ContentType =
                    "application/json";

                await context.Response.WriteAsJsonAsync(
                    new
                    {
                        message =
                            "Ocurrió un error interno en el servidor."
                    });
            }
        }
    }
}