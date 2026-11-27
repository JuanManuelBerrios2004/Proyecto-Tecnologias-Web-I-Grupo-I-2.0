using AutoMapper;
using ControlInventario.Core.DTOs;
using ControlInventario.Core.Entities;

namespace ControlInventario.Infrastructure.Mappings
{
    public class UsuarioProfile : Profile
    {
        public UsuarioProfile()
        {
            CreateMap<Usuario, UsuarioDto>().ReverseMap();
        }
    }
}