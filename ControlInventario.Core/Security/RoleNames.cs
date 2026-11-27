namespace ControlInventario.Core.Security
{
    public static class RoleNames
    {
        public const string Administrador = "Administrador";
        public const string Empleado = "Empleado";

        public const string AdministradorPolicy = "Administradores";
        public const string ConsultaStockPolicy = "ConsultaStock";
        public const string UsuarioAutenticadoPolicy = "UsuarioAutenticado";

        public static string? Normalizar(string? nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return null;
            }

            var valor = nombre.Trim();
            if (valor.Equals("Administrador", StringComparison.OrdinalIgnoreCase)
                || valor.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                || valor.Equals("Dueño", StringComparison.OrdinalIgnoreCase)
                || valor.Equals("Dueño / Administrador", StringComparison.OrdinalIgnoreCase)
                || valor.Equals("Propietario", StringComparison.OrdinalIgnoreCase))
            {
                return Administrador;
            }

            if (valor.Equals("Empleado", StringComparison.OrdinalIgnoreCase))
            {
                return Empleado;
            }

            return null;
        }
    }
}
