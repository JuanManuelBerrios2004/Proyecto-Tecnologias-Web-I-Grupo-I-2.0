using ControlInventario.Api.Filters;
using ControlInventario.Api.Security;
using ControlInventario.Core.Entities;
using ControlInventario.Core.Interfaces;
using ControlInventario.Infrastructure.Data;
using ControlInventario.Infrastructure.Mappings;
using ControlInventario.Infrastructure.Repositories;
using ControlInventario.Services.Interfaces;
using ControlInventario.Services.Services;
using ControlInventario.Services.Validators;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ControlInventario.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("ConnectionMySql");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("No se configuro ConnectionMySql.");
            }

            builder.Services.AddDbContext<InventarioContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

            builder.Services.AddControlInventarioAuthentication(builder.Configuration);
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
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            builder.Services.AddScoped<PasswordHasher<Usuario>>();
            builder.Services.AddScoped<IMovimientoInventarioService, MovimientoInventarioService>();
            builder.Services.AddScoped<ISalidaMercaderiaService, SalidaMercaderiaService>();

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();
            builder.Services.AddAutoMapper(typeof(ProductoProfile).Assembly);

            builder.Services.AddScoped<CrearProductoDtoValidator>();
            builder.Services.AddScoped<ActualizarProductoDtoValidator>();
            builder.Services.AddScoped<CrearCategoriaDtoValidator>();
            builder.Services.AddScoped<ActualizarCategoriaDtoValidator>();
            builder.Services.AddScoped<CrearMovimientoDtoValidator>();
            builder.Services.AddScoped<RegistrarEntradaDtoValidator>();
            builder.Services.AddScoped<RegistrarSalidaDtoValidator>();
            builder.Services.AddScoped<LoginDtoValidator>();
            builder.Services.AddScoped<ConsultaStockQueryDtoValidator>();

            var app = builder.Build();
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
