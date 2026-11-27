using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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

            var connectionString = builder.Configuration
                .GetConnectionString("ConnectionMySql");

            builder.Services.AddDbContext<InventarioContext>(options =>
            {
                options.UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString));
            });

            
            // * REPOSITORIOS EXISTENTES
             
            builder.Services.AddTransient<IPostRepository, PostRepository>();
            builder.Services.AddTransient<ICommentRepository, CommentRepository>();
            builder.Services.AddTransient<IUsuarioRepository, UsuarioRepository>();
            builder.Services.AddTransient<ICategoriaRepository, CategoriaRepository>();
            builder.Services.AddTransient<IProductoRepository, ProductoRepository>();

            
             //* REPOSITORIO Y SERVICE CU-03
             
            builder.Services.AddScoped<IMovimientoInventarioRepository, MovimientoInventarioRepository>();
            builder.Services.AddScoped<IMovimientoInventarioService, MovimientoInventarioService>();

           
            // * UNIT OF WORK, DAPPER Y SEGURIDAD (CU-01 y CU-08)
             
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IDapperContext, DapperContext>(); 
            builder.Services.AddScoped<IAuthService, AuthService>();

            
            // * CONFIGURACIÓN JWT BEARER (CU-01 y CU-08)
             
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Authentication:Issuer"],
                    ValidAudience = builder.Configuration["Authentication:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Authentication:SecretKey"]!))
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                        var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                        if (!string.IsNullOrEmpty(jti) && authService.IsTokenRevoked(jti))
                        {
                            context.Fail("El token ha sido revocado (sesión cerrada).");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();
            builder.Services.AddAutoMapper(typeof(PostProfile).Assembly);

           
            // VALIDADORES FLUENTVALIDATION
            
            builder.Services.AddScoped<PostDtoValidator>();
            builder.Services.AddScoped<CrearPostDtoValidator>();
            builder.Services.AddScoped<ActualizarPostDtoValidator>();
            builder.Services.AddScoped<CrearProductoDtoValidator>();
            builder.Services.AddScoped<ActualizarProductoDtoValidator>();
            builder.Services.AddScoped<CategoriaDtoValidator>();
            builder.Services.AddScoped<ProductoDtoValidator>();
            builder.Services.AddScoped<CrearMovimientoDtoValidator>();
            builder.Services.AddScoped<RegistrarEntradaDtoValidator>();
            builder.Services.AddScoped<LoginDtoValidator>();
            builder.Services.AddScoped<ConsultaStockQueryDtoValidator>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            
             //* MIDDLEWARE GLOBAL DE EXCEPCIONES
             
            app.UseMiddleware<ExceptionMiddleware>();

            app.UseHttpsRedirection();

            app.UseAuthentication(); 
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}