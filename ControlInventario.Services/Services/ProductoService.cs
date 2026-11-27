using System.Net;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using ControlInventario.Core.DTOs;
using ControlInventario.Core.Entities;
using ControlInventario.Core.Exceptions;
using ControlInventario.Core.Interfaces;
using ControlInventario.Services.Interfaces;
using ControlInventario.Services.Reglas;

namespace ControlInventario.Services.Services
{
    public class ProductoService : IProductoService
    {
        private const string MensajeDuplicado = "Ya existe un producto registrado con ese nombre.";
        private const string MensajeCategoriaInvalida = "La categoría indicada no existe.";
        private const string MensajeUsuarioInvalido = "Se requiere una identidad de usuario autenticada válida.";
        private const string MensajeConflicto = "El producto fue modificado o ya no está disponible. Vuelva a consultarlo.";
        private readonly IUnitOfWork _unitOfWork;

        public ProductoService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task<IEnumerable<Producto>> GetProductosAsync(string? nombre, int? categoriaId, bool? soloActivos)
        {
            return _unitOfWork.ProductoRepository.GetProductosAsync(nombre, categoriaId, soloActivos);
        }

        public async Task<Producto> GetProductoByIdAsync(int id)
        {
            return await _unitOfWork.ProductoRepository.GetProductoByIdAsync(id)
                ?? throw new BusinessException("Producto no encontrado.", HttpStatusCode.NotFound);
        }

        public Task<IEnumerable<ProductoStockDto>> GetStockProductosAsync(string? nombre, int? categoriaId)
        {
            return _unitOfWork.ProductoRepository.GetStockProductosAsync(nombre, categoriaId);
        }

        public async Task<Producto> InsertProducto(Producto producto)
        {
            await ValidarCatalogoAsync(producto);
            producto.StockActual = 0;
            producto.Activo = true;

            await _unitOfWork.ProductoRepository.Add(producto);
            await GuardarProductoAsync(_unitOfWork.SaveChangesAsync);
            return await GetProductoByIdAsync(producto.Id);
        }

        public async Task<Producto> UpdateProducto(Producto cambios, int usuarioId)
        {
            await PersistirModificacionAuditadaAsync(cambios.Id, usuarioId, cambios);
            return await GetProductoByIdAsync(cambios.Id);
        }

        public Task DesactivarProductoAsync(int id, int usuarioId)
        {
            return PersistirModificacionAuditadaAsync(id, usuarioId, cambios: null);
        }

        private async Task PersistirModificacionAuditadaAsync(int id, int usuarioId, Producto? cambios)
        {
            if (usuarioId <= 0 || !await _unitOfWork.UsuarioRepository.ExisteUsuarioAsync(usuarioId))
            {
                throw new BusinessException(MensajeUsuarioInvalido, HttpStatusCode.Unauthorized);
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var producto = await _unitOfWork.ProductoRepository.ObtenerParaModificarAsync(id)
                    ?? throw new BusinessException("Producto no encontrado.", HttpStatusCode.NotFound);

                var campos = cambios == null
                    ? PrepararDesactivacion(producto)
                    : await PrepararEdicionAsync(producto, cambios);

                if (campos.Count == 0)
                {
                    await _unitOfWork.RollbackAsync();
                    return;
                }

                await _unitOfWork.HistorialProductoRepository.AgregarAsync(campos.Select(campo =>
                    new HistorialProducto
                    {
                        ProductoId = producto.Id,
                        UsuarioId = usuarioId,
                        CampoModificado = campo
                    }));

                // CommitAsync llama a SaveChangesAsync una vez y confirma la transacción que mantiene el bloqueo.
                await GuardarProductoAsync(_unitOfWork.CommitAsync);
            }
            catch (Exception ex) when (ErrorMySql.Coincide(ex, 1205) || ErrorMySql.Coincide(ex, 1213))
            {
                await _unitOfWork.RollbackAsync();
                throw new BusinessException(MensajeConflicto, HttpStatusCode.Conflict);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        private async Task<IReadOnlyList<string>> PrepararEdicionAsync(Producto producto, Producto cambios)
        {
            await ValidarCatalogoAsync(cambios, producto.Id);

            var campos = new List<string>(4);
            if (!string.Equals(producto.Nombre, cambios.Nombre, StringComparison.Ordinal))
                campos.Add("nombre");
            if (producto.CategoriaId != cambios.CategoriaId)
                campos.Add("categoria_id");
            if (producto.PrecioCompra != cambios.PrecioCompra)
                campos.Add("precio_compra");
            if (producto.PrecioVenta != cambios.PrecioVenta)
                campos.Add("precio_venta");

            if (campos.Count != 0)
            {
                _unitOfWork.ProductoRepository.UpdateProducto(producto, cambios);
            }

            return campos;
        }

        private IReadOnlyList<string> PrepararDesactivacion(Producto producto)
        {
            if (!producto.Activo)
            {
                return [];
            }

            _unitOfWork.ProductoRepository.DesactivarProducto(producto);
            return ["activo"];
        }

        private async Task ValidarCatalogoAsync(Producto producto, int? excluirId = null)
        {
            producto.Nombre = producto.Nombre.Trim();

            if (!await _unitOfWork.CategoriaRepository.ExisteCategoriaAsync(producto.CategoriaId))
            {
                throw new BusinessException(MensajeCategoriaInvalida);
            }

            if (await _unitOfWork.ProductoRepository.ExisteNombreAsync(producto.Nombre, excluirId))
            {
                throw new BusinessException(MensajeDuplicado, HttpStatusCode.Conflict);
            }

            if (!ReglasProducto.PrecioVentaValido(producto.PrecioCompra, producto.PrecioVenta))
            {
                throw new BusinessException(ReglasProducto.MensajePrecioVenta);
            }
        }

        private static async Task GuardarProductoAsync(Func<Task> guardar)
        {
            try
            {
                await guardar();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new BusinessException(MensajeConflicto, HttpStatusCode.Conflict);
            }
            catch (DbUpdateException ex) when (ErrorMySql.Coincide(ex, 1062, EsIndiceDelNombre))
            {
                throw new BusinessException(MensajeDuplicado, HttpStatusCode.Conflict);
            }
            catch (DbUpdateException ex) when (ErrorMySql.Coincide(ex, 1452, error =>
                EsReferencia(error, "fk_productos_categorias", "productos", "categoria_id", "categorias")))
            {
                throw new BusinessException(MensajeCategoriaInvalida);
            }
            catch (DbUpdateException ex) when (ErrorMySql.Coincide(ex, 1452, error =>
                EsReferencia(error, "fk_historial_usuarios", "historial_productos", "usuario_id", "usuarios")))
            {
                throw new BusinessException(MensajeUsuarioInvalido, HttpStatusCode.Unauthorized);
            }
            catch (DbUpdateException ex) when (ErrorMySql.Coincide(ex, 1452, error =>
                EsReferencia(error, "fk_historial_productos", "historial_productos", "producto_id", "productos")))
            {
                throw new BusinessException(MensajeConflicto, HttpStatusCode.Conflict);
            }
            catch (DbUpdateException ex) when (ErrorMySql.Coincide(ex, 3819, EsRestriccionDePrecio)
                || ErrorMySql.Coincide(ex, 4025, EsRestriccionDePrecio))
            {
                throw new BusinessException(ReglasProducto.MensajePrecioVenta);
            }
        }

        private static bool EsIndiceDelNombre(MySqlException error)
        {
            var mensaje = error.Message.TrimEnd();
            return mensaje.EndsWith("'nombre'", StringComparison.OrdinalIgnoreCase)
                || mensaje.EndsWith("'productos.nombre'", StringComparison.OrdinalIgnoreCase)
                || mensaje.EndsWith("'IX_productos_Nombre'", StringComparison.OrdinalIgnoreCase)
                || mensaje.EndsWith("'productos.IX_productos_Nombre'", StringComparison.OrdinalIgnoreCase);
        }

        private static bool EsReferencia(
            MySqlException error, string restriccion, string tablaOrigen, string columna, string tablaDestino)
        {
            return error.Message.Contains($"`{restriccion}`", StringComparison.OrdinalIgnoreCase)
                || (error.Message.Contains($"`{tablaOrigen}`", StringComparison.OrdinalIgnoreCase)
                    && error.Message.Contains($"FOREIGN KEY (`{columna}`) REFERENCES `{tablaDestino}` (`id`)",
                        StringComparison.OrdinalIgnoreCase));
        }

        private static bool EsRestriccionDePrecio(MySqlException error)
        {
            return error.Message.Contains("'chk_precio_venta'", StringComparison.OrdinalIgnoreCase)
                || error.Message.Contains("`chk_precio_venta`", StringComparison.OrdinalIgnoreCase);
        }
    }
}
