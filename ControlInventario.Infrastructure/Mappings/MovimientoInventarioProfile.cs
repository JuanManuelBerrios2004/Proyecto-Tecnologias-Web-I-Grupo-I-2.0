using AutoMapper;
using ControlInventario.Core.DTOs;
using ControlInventario.Core.Entities;
using System;

namespace ControlInventario.Infrastructure.Mappings
{
    /// Mapeos del CU-04 (Registrar Salida de Mercadería).
    public class MovimientoInventarioProfile : Profile
    {
        public MovimientoInventarioProfile()
        {
            // DTO de entrada -> entidad. El backend controla tipo, fecha y valores calculados.
            CreateMap<RegistrarSalidaDto, MovimientoInventario>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TipoMovimiento, opt => opt.MapFrom(_ => "SALIDA"))
                .ForMember(dest => dest.Motivo,
                    opt => opt.MapFrom(src => src.Motivo.Trim().ToUpperInvariant()))
                .ForMember(dest => dest.Observaciones, opt => opt.MapFrom(src => src.Observacion))
                .ForMember(dest => dest.FechaRegistro, opt => opt.MapFrom(_ => DateTime.UtcNow))
                // No se persiste: lo calcula el repositorio al descontar el stock.
                .ForMember(dest => dest.StockResultante, opt => opt.Ignore())
                .ForMember(dest => dest.Producto, opt => opt.Ignore())
                .ForMember(dest => dest.Usuario, opt => opt.Ignore());

            // Entidad -> DTO de salida (incluye StockResultante).
            CreateMap<MovimientoInventario, MovimientoInventarioDto>();

            // Paso 3 del CU-04: stock actual y precio de venta sugerido del producto seleccionado.
            CreateMap<Producto, ProductoSalidaInfoDto>()
                .ForMember(dest => dest.ProductoId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.PrecioVentaSugerido, opt => opt.MapFrom(src => src.PrecioVenta))
                .ForMember(dest => dest.CategoriaNombre,
                    opt => opt.MapFrom(src => src.Categoria != null ? src.Categoria.Nombre : null));
        }
    }
}