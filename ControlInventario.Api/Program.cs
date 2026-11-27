using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using ControlInventario.Api.Filters;

using ControlInventario.Core.Interfaces;

using ControlInventario.Infrastructure.Data;
using ControlInventario.Infrastructure.Mappings;
using ControlInventario.Infrastructure.Repositories;

using ControlInventario.Services.Interfaces;
using ControlInventario.Services.Services;
using ControlInventario.Services.Validators;
using ControlInventario.Core.Security;
using ControlInventario.Core.CustomEntities;
using ControlInventario.Api.Security;

namespace ControlInventario.Api
{
    public class Program
    {
        public static void Main(
            string[] args)
        {
            var builder =
                WebApplication.CreateBuilder(args);

            var connectionString =
                builder.Configuration
                    .GetConnectionString(
                        "ConnectionMySql");

            builder.Services.AddDbContext<
                InventarioContext>(
                    options =>
                    {
                        options.UseMySql(
                            connectionString,
                            ServerVersion.AutoDetect(
                                connectionString));
                    });

            builder.Services.AddControlInventarioAuthentication(
                builder.Configuration);

            builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            builder.Services.AddScoped<IUsuarioRepository>(services =>
                services.GetRequiredService<IUnitOfWork>().UsuarioRepository);
            builder.Services.AddScoped<ICategoriaRepository>(services =>
                services.GetRequiredService<IUnitOfWork>().CategoriaRepository);
            builder.Services.AddScoped<IProductoRepository>(services =>
                services.GetRequiredService<IUnitOfWork>().ProductoRepository);
            builder.Services.AddScoped<IMovimientoInventarioRepository>(services =>
                services.GetRequiredService<IUnitOfWork>().MovimientoInventarioRepository);

            builder.Services.AddTransient<IProductoService, ProductoService>();
            builder.Services.AddTransient<ICategoriaService, CategoriaService>();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            builder.Services.AddScoped<PasswordHasher<ControlInventario.Core.Entities.Usuario>>();

            /*
             * SERVICE CU-03
             */

            builder.Services.AddScoped<
                IMovimientoInventarioService,
                MovimientoInventarioService>();

            builder.Services.AddScoped<ISalidaMercaderiaService, SalidaMercaderiaService>();

            builder.Services.AddControllers();

            builder.Services.AddOpenApi();

            builder.Services.AddAutoMapper(
                typeof(ProductoProfile).Assembly);

            /*
             * VALIDADORES EXISTENTES
             */

            builder.Services.AddScoped<
                CrearProductoDtoValidator>();

            builder.Services.AddScoped<
                ActualizarProductoDtoValidator>();

            builder.Services.AddScoped<CrearCategoriaDtoValidator>();
            builder.Services.AddScoped<ActualizarCategoriaDtoValidator>();

            builder.Services.AddScoped<
                CrearMovimientoDtoValidator>();

            /*
             * VALIDADOR CU-03
             */

            builder.Services.AddScoped<
                RegistrarEntradaDtoValidator>();

            builder.Services.AddScoped<RegistrarSalidaDtoValidator>();
            builder.Services.AddScoped<LoginDtoValidator>();
            builder.Services.AddScoped<ConsultaStockQueryDtoValidator>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            /*
             * MIDDLEWARE GLOBAL
             * DE EXCEPCIONES
             */

            app.UseMiddleware<
                ExceptionHandlingMiddleware>();

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}