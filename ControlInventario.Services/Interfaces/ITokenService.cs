using ControlInventario.Core.Entities;

namespace ControlInventario.Services.Interfaces
{
    public interface ITokenService
    {
        (string Token, DateTime ExpiraEn) Generar(Usuario usuario, string rol);
    }
}
