using Microsoft.AspNetCore.Identity;
using ControlInventario.Core.Entities;

namespace ControlInventario.Cu02.Verificaciones
{
    internal sealed class ResultadoDatosSinteticos
    {
        public required IReadOnlyList<Rol> Roles { get; init; }
        public required IReadOnlyList<Usuario> Usuarios { get; init; }
        public required IReadOnlyList<Categoria> Categorias { get; init; }
        public required IReadOnlyList<Producto> Productos { get; init; }
        public required IReadOnlyDictionary<string, string> Contrasenas { get; init; }
    }

    internal sealed class DatosSinteticos
    {
        public static ResultadoDatosSinteticos Preparar(
            PasswordHasher<Usuario> hasher)
        {
            var contrasenas = new Dictionary<string, string>
            {
                ["Administrador"] = "Integracion-Solo-Prueba-Admin",
                ["Empleado"] = "Integracion-Solo-Prueba-Empleado",
                ["Inactivo"] = "Integracion-Solo-Prueba-Inactivo",
                ["Desconocido"] = "Integracion-Solo-Prueba-Desconocido"
            };

            var administradorRol = new Rol { Nombre = "Administrador" };
            var empleadoRol = new Rol { Nombre = "Empleado" };
            var desconocidoRol = new Rol { Nombre = "RolDesconocido" };

            var administrador = new Usuario
            {
                NombreCompleto = "Administrador de integración",
                Email = "administrador.integracion@test.local",
                Activo = true,
                Rol = administradorRol
            };
            administrador.PasswordHash =
                hasher.HashPassword(administrador, contrasenas["Administrador"]);

            var empleado = new Usuario
            {
                NombreCompleto = "Empleado de integración",
                Email = "empleado.integracion@test.local",
                Activo = true,
                Rol = empleadoRol
            };
            empleado.PasswordHash =
                hasher.HashPassword(empleado, contrasenas["Empleado"]);

            var inactivo = new Usuario
            {
                NombreCompleto = "Usuario inactivo de integración",
                Email = "inactivo.integracion@test.local",
                Activo = false,
                Rol = empleadoRol
            };
            inactivo.PasswordHash =
                hasher.HashPassword(inactivo, contrasenas["Inactivo"]);

            var desconocido = new Usuario
            {
                NombreCompleto = "Rol desconocido de integración",
                Email = "desconocido.integracion@test.local",
                Activo = true,
                Rol = desconocidoRol
            };
            desconocido.PasswordHash =
                hasher.HashPassword(desconocido, contrasenas["Desconocido"]);

            var sinProductos = new Categoria
            {
                Nombre = "Categoria integracion sin productos",
                Descripcion = "Datos sintéticos"
            };
            var conActivos = new Categoria
            {
                Nombre = "Categoria integracion con activos",
                Descripcion = "Datos sintéticos"
            };
            var conInactivos = new Categoria
            {
                Nombre = "Categoria integracion con inactivos",
                Descripcion = "Datos sintéticos"
            };

            var productos = new[]
            {
                new Producto
                {
                    Nombre = "Producto integracion activo positivo",
                    Categoria = conActivos,
                    CategoriaId = 0,
                    PrecioCompra = 10m,
                    PrecioVenta = 15m,
                    StockActual = 5,
                    Activo = true
                },
                new Producto
                {
                    Nombre = "Producto integracion activo cero",
                    Categoria = conActivos,
                    CategoriaId = 0,
                    PrecioCompra = 10m,
                    PrecioVenta = 15m,
                    StockActual = 0,
                    Activo = true
                },
                new Producto
                {
                    Nombre = "Producto integracion inactivo positivo",
                    Categoria = conInactivos,
                    CategoriaId = 0,
                    PrecioCompra = 10m,
                    PrecioVenta = 15m,
                    StockActual = 5,
                    Activo = false
                },
                new Producto
                {
                    Nombre = "Producto integracion inactivo cero",
                    Categoria = conInactivos,
                    CategoriaId = 0,
                    PrecioCompra = 10m,
                    PrecioVenta = 15m,
                    StockActual = 0,
                    Activo = false
                }
            };

            return new ResultadoDatosSinteticos
            {
                Roles = [administradorRol, empleadoRol, desconocidoRol],
                Usuarios = [administrador, empleado, inactivo, desconocido],
                Categorias = [sinProductos, conActivos, conInactivos],
                Productos = productos,
                Contrasenas = contrasenas
            };
        }
    }
}
