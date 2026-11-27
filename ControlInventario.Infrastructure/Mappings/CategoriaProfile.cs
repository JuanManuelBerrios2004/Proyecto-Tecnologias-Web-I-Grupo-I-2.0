using AutoMapper;
using ControlInventario.Core.DTOs;
using ControlInventario.Core.Entities;

namespace ControlInventario.Infrastructure.Mappings
{
    public class CategoriaProfile : Profile
    {
        public CategoriaProfile()
        {
            CreateMap<Categoria, CategoriaDto>();

            CreateMap<CrearCategoriaDto, Categoria>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreadoEn, opt => opt.Ignore())
                .ForMember(dest => dest.Productos, opt => opt.Ignore());

            CreateMap<ActualizarCategoriaDto, Categoria>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreadoEn, opt => opt.Ignore())
                .ForMember(dest => dest.Productos, opt => opt.Ignore());
        }
    }
}
