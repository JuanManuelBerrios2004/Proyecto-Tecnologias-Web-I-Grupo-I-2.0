using AutoMapper;
using ControlInventario.Core.DTOs;
using ControlInventario.Core.Entities;

namespace ControlInventario.Infrastructure.Mappings
{
	public class ProductoProfile : Profile
	{
		public ProductoProfile()
		{
			// Producto -> ProductoDto
			CreateMap<Producto, ProductoDto>()
				.ForMember(dest => dest.CategoriaNombre,
					opt => opt.MapFrom(src =>
						src.Categoria != null
							? src.Categoria.Nombre
							: null));

			// Producto -> ProductoStockDto
			CreateMap<Producto, ProductoStockDto>()
				.ForMember(dest => dest.CategoriaNombre,
					opt => opt.MapFrom(src =>
						src.Categoria != null
							? src.Categoria.Nombre
							: null));

			// CrearProductoDto -> Producto
			CreateMap<CrearProductoDto, Producto>()
				.ForMember(dest => dest.Id,
					opt => opt.Ignore())
				.ForMember(dest => dest.StockActual,
					opt => opt.MapFrom(src => 0))
				.ForMember(dest => dest.Activo,
					opt => opt.MapFrom(src => true))
				.ForMember(dest => dest.CreadoEn,
					opt => opt.MapFrom(src => DateTime.UtcNow))
				.ForMember(dest => dest.ActualizadoEn,
					opt => opt.MapFrom(src => DateTime.UtcNow))
				.ForMember(dest => dest.Descripcion,
					opt => opt.Ignore())
				.ForMember(dest => dest.Categoria,
					opt => opt.Ignore())
				.ForMember(dest => dest.Movimientos,
					opt => opt.Ignore());

			// ActualizarProductoDto -> Producto
			CreateMap<ActualizarProductoDto, Producto>()
				.ForMember(dest => dest.Id,
					opt => opt.Ignore())
				.ForMember(dest => dest.StockActual,
					opt => opt.Ignore())
				.ForMember(dest => dest.Activo,
					opt => opt.Ignore())
				.ForMember(dest => dest.CreadoEn,
					opt => opt.Ignore())
				.ForMember(dest => dest.ActualizadoEn,
					opt => opt.MapFrom(src => DateTime.UtcNow))
				.ForMember(dest => dest.Descripcion,
					opt => opt.Ignore())
				.ForMember(dest => dest.Categoria,
					opt => opt.Ignore())
				.ForMember(dest => dest.Movimientos,
					opt => opt.Ignore());
		}
	}
}