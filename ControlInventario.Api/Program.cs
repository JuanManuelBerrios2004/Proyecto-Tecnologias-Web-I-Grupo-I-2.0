using Microsoft.EntityFrameworkCore;
using ControlInventario.Core.Interfaces;
using ControlInventario.Infrastructure.Data;
using ControlInventario.Infrastructure.Mappings;
using ControlInventario.Infrastructure.Repositories;
using ControlInventario.Services.Validators;

namespace ControlInventario.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString =
                builder.Configuration
                    .GetConnectionString("ConnectionMySql");

            builder.Services.AddDbContext<InventarioContext>(options =>
                options.UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString)));

            builder.Services.AddTransient<IPostRepository, PostRepository>();
            builder.Services.AddTransient<ICommentRepository, CommentRepository>();
            builder.Services.AddTransient<IUsuarioRepository, UsuarioRepository>();
            builder.Services.AddTransient<ICategoriaRepository, CategoriaRepository>();
            builder.Services.AddTransient<IProductoRepository, ProductoRepository>();

            builder.Services.AddTransient<
                IMovimientoInventarioRepository,
                MovimientoInventarioRepository>();

            builder.Services.AddControllers();

            builder.Services.AddOpenApi();

            builder.Services.AddAutoMapper(
                typeof(PostProfile).Assembly);

            builder.Services.AddScoped<PostDtoValidator>();
            builder.Services.AddScoped<CrearPostDtoValidator>();
            builder.Services.AddScoped<ActualizarPostDtoValidator>();

            builder.Services.AddScoped<CrearProductoDtoValidator>();
            builder.Services.AddScoped<ActualizarProductoDtoValidator>();

            builder.Services.AddScoped<CategoriaDtoValidator>();
            builder.Services.AddScoped<ProductoDtoValidator>();

            builder.Services.AddScoped<CrearMovimientoDtoValidator>();

            // CU-03
            builder.Services.AddScoped<RegistrarEntradaDtoValidator>();

            builder.Services.AddScoped<LoginDtoValidator>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}