using MySqlConnector;

namespace ControlInventario.Services.Services
{
    internal static class ErrorMySql
    {
        internal static bool Coincide(
            Exception exception,
            int numero,
            Func<MySqlException, bool>? condicion = null)
        {
            for (Exception? actual = exception; actual != null; actual = actual.InnerException)
            {
                if (actual is MySqlException error && error.Number == numero
                    && (condicion == null || condicion(error)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
