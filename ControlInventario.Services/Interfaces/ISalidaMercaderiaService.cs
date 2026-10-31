using System.Net;
using ControlInventario.Core.DTOs;
using ControlInventario.Core.Entities;
using ControlInventario.Core.Exceptions;
using ControlInventario.Core.Interfaces;
using ControlInventario.Services.Interfaces;

namespace ControlInventario.Services.Services
{
    public class SalidaMercaderiaService : ISalidaMercaderiaService
    {
        private readonly IProductoRepository _productoRepository;
        private readonly IMovimientoInventarioRepository _movimientoRepository;

        public SalidaMercaderiaService(
            IProductoRepository productoRepository,
            IMovimientoInventarioRepository movimientoRepository)
        {
            _productoRepository = productoRepository;
            _movimientoRepository = movimientoRepository;
        }

        public async Task<Producto> ObtenerProductoParaSalidaAsync(int productoId)
        {
            if (productoId <= 0)
                throw new BusinessException("El ID del producto debe ser mayor que cero.");

            var producto = await _productoRepository.GetProductoByIdAsync(productoId);

            if (producto == null)
                throw new BusinessException("Producto no encontrado.", HttpStatusCode.NotFound);

            // RN-08: un producto desactivado no admite movimientos.
            if (!producto.Activo)
                throw new BusinessException("El producto está desactivado; no admite salidas de mercadería.");

            // Precondición del CU-04: el producto debe tener stock registrado mayor a cero.
            if (producto.StockActual <= 0)
                throw new BusinessException(
                    "El producto no cuenta con stock disponible para registrar una salida.",
                    HttpStatusCode.Conflict,
                    new { stockDisponible = producto.StockActual });

            return producto;
        }

        public async Task<MovimientoInventario> RegistrarSalidaAsync(MovimientoInventario salida)
        {
            // Este caso de uso solo registra salidas, sin importar lo que llegue desde la capa web.
            salida.TipoMovimiento = "SALIDA";

            if (salida.Cantidad <= 0)
                throw new BusinessException("La cantidad a retirar debe ser mayor que cero.");

            // Paso 4: el motivo solo puede ser «Venta» o «Ajuste por Merma / Daño».
            if (!MotivosSalida.Permitidos.Contains(salida.Motivo, StringComparer.OrdinalIgnoreCase))
                throw new BusinessException("El motivo de una salida debe ser Venta o Ajuste por Merma / Daño.");

            // Precondiciones: el producto existe, está activo y tiene stock mayor a cero.
            var producto = await ObtenerProductoParaSalidaAsync(salida.ProductoId);

            // Paso 6: la cantidad a retirar no puede superar las existencias actuales.
            if (salida.Cantidad > producto.StockActual)
                throw StockInsuficiente(salida.Cantidad, producto.StockActual);

            // Paso 7: el repositorio descuenta el stock y guarda el movimiento en una sola
            // transacción. La comprobación de stock se repite allí de forma atómica por si otra
            // salida se registró mientras tanto.
            var resultado = await _movimientoRepository.RegistrarMovimientoAsync(salida);

            if (!resultado.Registrado)
                throw ConvertirRechazo(resultado, salida.Cantidad);

            salida.StockResultante = resultado.StockResultante;
            return salida;
        }

        /// <summary>
        /// Traduce el rechazo del repositorio a una excepción de negocio.
        /// </summary>
        private static BusinessException ConvertirRechazo(ResultadoRegistroMovimiento resultado, int cantidadSolicitada)
        {
            return resultado.Motivo switch
            {
                MotivoRechazoMovimiento.ProductoNoEncontrado =>
                    new BusinessException("Producto no encontrado.", HttpStatusCode.NotFound),

                MotivoRechazoMovimiento.UsuarioNoEncontrado =>
                    new BusinessException("Usuario no encontrado.", HttpStatusCode.NotFound),

                MotivoRechazoMovimiento.ProductoInactivo =>
                    new BusinessException("El producto está desactivado; no admite salidas de mercadería."),

                MotivoRechazoMovimiento.StockInsuficiente =>
                    StockInsuficiente(cantidadSolicitada, resultado.StockDisponible),

                _ => new BusinessException("No fue posible registrar la salida de mercadería.")
            };
        }

        private static BusinessException StockInsuficiente(int cantidadSolicitada, int stockDisponible)
        {
            return new BusinessException(
                $"La cantidad a retirar ({cantidadSolicitada}) supera las existencias actuales ({stockDisponible}).",
                HttpStatusCode.Conflict,
                new { cantidadSolicitada, stockDisponible });
        }
    }
}