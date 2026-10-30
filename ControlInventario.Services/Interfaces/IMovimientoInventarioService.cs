using ControlInventario.Core.DTOs;

namespace ControlInventario.Services.Interfaces
{
    public interface IMovimientoInventarioService
    {
        Task<MovimientoInventarioDto> RegistrarEntradaAsync(
            RegistrarEntradaDto entradaDto);

        Task<MovimientoInventarioDto> RegistrarMovimientoAsync(
            CrearMovimientoDto movimientoDto);

        Task<MovimientoInventarioDto> ObtenerPorIdAsync(
            int id);
    }
}