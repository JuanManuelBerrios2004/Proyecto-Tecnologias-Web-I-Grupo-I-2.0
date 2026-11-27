
using Microsoft.EntityFrameworkCore;

using ControlInventario.Api.Middlewares;
using ControlInventario.Core.Interfaces;
using ControlInventario.Infrastructure.Data;
using ControlInventario.Infrastructure.Mappings;
using ControlInventario.Infrastructure.Repositories;
using ControlInventario.Services.Interfaces;
using ControlInventario.Services.Services;
using ControlInventario.Services.Validators;

namespace ControlInventario.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // CONFIGURACION DE MYSQL
            var connectionString = builder.Configuration
                .GetConnectionString("ConnectionMySql");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "No se configuro ConnectionMySql.");
            }

            builder.Services.AddDbContext<InventarioContext>(
                options => options.UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString)
                )
            );

            // REPOSITORIOS
            builder.Services.AddTransient<
                IUsuarioRepository,
                UsuarioRepository>();

            builder.Services.AddTransient<
                ICategoriaRepository,
                CategoriaRepository>();

            builder.Services.AddTransient<
                IProductoRepository,
                ProductoRepository>();

            builder.Services.AddScoped<
                IMovimientoInventarioRepository,
                MovimientoInventarioRepository>();

            // SERVICIOS CU-03 Y CU-04
            builder.Services.AddScoped<
                IMovimientoInventarioService,
                MovimientoInventarioService>();

            builder.Services.AddScoped<
                ISalidaMercaderiaService,
                SalidaMercaderiaService>();

            // CONTROLADORES Y OPENAPI
            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            // AUTOMAPPER
            builder.Services.AddAutoMapper(
                typeof(ProductoProfile).Assembly);

            // VALIDADORES
            builder.Services.AddScoped<CrearProductoDtoValidator>();
            builder.Services.AddScoped<ActualizarProductoDtoValidator>();
            builder.Services.AddScoped<CategoriaDtoValidator>();
            builder.Services.AddScoped<ProductoDtoValidator>();
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

            // MANEJO GLOBAL DE ERRORES
            app.UseMiddleware<ExceptionMiddleware>();

            app.UseHttpsRedirection();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
