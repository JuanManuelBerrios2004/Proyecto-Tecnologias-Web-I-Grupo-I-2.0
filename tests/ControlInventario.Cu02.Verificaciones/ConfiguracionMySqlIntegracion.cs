using MySqlConnector;

namespace ControlInventario.Cu02.Verificaciones
{
    internal sealed class ConfiguracionMySqlIntegracion
    {
        public const string VariableEntorno = "CONTROLINVENTARIO_TEST_MYSQL";
        public const string BaseAutorizada = "inventario_cu02_cu05_cu06_test";
        public const string UsuarioAutorizado = "inventario_integracion";
        public const string UsuarioPreparacion = "inventario_integracion_prep";
        public const uint PuertoAutorizado = 3306;

        public static MySqlConnectionStringBuilder Validar(
            string? configuracion,
            bool permitirUsuarioPreparacion = false)
        {
            if (string.IsNullOrWhiteSpace(configuracion))
                throw new InvalidOperationException("Debe proporcionar explícitamente la configuración MySQL de integración.");

            MySqlConnectionStringBuilder builder;
            try
            {
                builder = new MySqlConnectionStringBuilder(configuracion);
            }
            catch (ArgumentException)
            {
                throw new InvalidOperationException("La configuración MySQL de integración no tiene un formato válido.");
            }

            if (!EsServidorLocal(builder.Server))
                throw new InvalidOperationException("La integración solo permite servidores MySQL locales.");
            if (builder.Port != PuertoAutorizado)
                throw new InvalidOperationException("La integración solo permite el puerto MySQL autorizado.");
            if (!string.Equals(builder.Database, BaseAutorizada, StringComparison.Ordinal))
                throw new InvalidOperationException("La configuración debe apuntar exclusivamente a la base de integración autorizada.");

            var usuario = builder.UserID?.Trim();
            if (string.IsNullOrWhiteSpace(usuario)
                || EsUsuarioAdministrativo(usuario)
                || (!string.Equals(usuario, UsuarioAutorizado, StringComparison.OrdinalIgnoreCase)
                    && !(permitirUsuarioPreparacion
                        && string.Equals(usuario, UsuarioPreparacion, StringComparison.OrdinalIgnoreCase))))
            {
                throw new InvalidOperationException("La configuración debe usar el usuario dedicado de integración.");
            }

            if (string.IsNullOrWhiteSpace(builder.Password))
                throw new InvalidOperationException("La configuración de integración debe incluir su contraseña local.");

            return builder;
        }

        private static bool EsServidorLocal(string? servidor) =>
            string.Equals(servidor?.Trim(), "127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(servidor?.Trim(), "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(servidor?.Trim(), "::1", StringComparison.OrdinalIgnoreCase);

        private static bool EsUsuarioAdministrativo(string usuario) =>
            usuario.Equals("root", StringComparison.OrdinalIgnoreCase)
            || usuario.Equals("administrator", StringComparison.OrdinalIgnoreCase)
            || usuario.Equals("admin", StringComparison.OrdinalIgnoreCase)
            || usuario.Equals("mysql.sys", StringComparison.OrdinalIgnoreCase)
            || usuario.Equals("mysql.session", StringComparison.OrdinalIgnoreCase)
            || usuario.Equals("mysql.infoschema", StringComparison.OrdinalIgnoreCase);
    }
}
