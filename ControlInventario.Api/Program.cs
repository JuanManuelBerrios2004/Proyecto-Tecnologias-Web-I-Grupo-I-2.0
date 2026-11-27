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

            var jwtSettings = builder.Configuration
                .GetSection(JwtSettings.SectionName)
                .Get<JwtSettings>() ?? new JwtSettings();
            jwtSettings.Key = builder.Configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtSettings.Key)
                || Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32
                || string.IsNullOrWhiteSpace(jwtSettings.Issuer)
                || string.IsNullOrWhiteSpace(jwtSettings.Audience)
                || jwtSettings.ExpirationMinutes <= 0)
            {
                throw new InvalidOperationException(
                    "La configuración JWT debe incluir una clave de al menos 32 bytes, issuer, audience y expiración positiva.");
            }

            builder.Services.Configure<JwtSettings>(
                builder.Configuration.GetSection(JwtSettings.SectionName));
            builder.Services.PostConfigure<JwtSettings>(options =>
            {
                options.Key = builder.Configuration["Jwt:Key"];
            });

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false;
                    options.RequireHttpsMetadata = true;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtSettings.Key)),
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtSettings.Audience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1),
                        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                        NameClaimType = ClaimTypes.Name,
                        RoleClaimType = ClaimTypes.Role
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var identificadores = context.Principal?.FindAll(
                                    ClaimTypes.NameIdentifier)
                                .Select(claim => claim.Value)
                                .Distinct(StringComparer.Ordinal)
                                .ToArray();

                            var roles = context.Principal?.FindAll(ClaimTypes.Role)
                                .Select(claim => claim.Value)
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .ToArray();

                            if (identificadores is not { Length: 1 }
                                || !int.TryParse(identificadores[0], out var usuarioId)
                                || usuarioId <= 0
                                || roles is not { Length: 1 })
                            {
                                context.Fail("Identidad de usuario inválida.");
                                return;
                            }

                            using var scope = context.HttpContext
                                .RequestServices.CreateScope();
                            var unitOfWork = scope.ServiceProvider
                                .GetRequiredService<IUnitOfWork>();
                            var usuario = await unitOfWork.UsuarioRepository
                                .GetUsuarioByIdAsync(usuarioId);
                            var rolPersistido = RoleNames.Normalizar(usuario?.Rol?.Nombre);

                            if (usuario == null
                                || !usuario.Activo
                                || rolPersistido == null
                                || !string.Equals(rolPersistido, roles[0],
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                context.Fail("Usuario o rol no autorizado.");
                            }
                        },
                        OnChallenge = async context =>
                        {
                            context.HandleResponse();
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync(JsonSerializer.Serialize(
                                new ErrorResponse
                                {
                                    Status = StatusCodes.Status401Unauthorized,
                                    Title = "Unauthorized",
                                    Message = "No autenticado.",
                                    TraceId = context.HttpContext.TraceIdentifier
                                },
                                new JsonSerializerOptions
                                {
                                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                }));
                        },
                        OnForbidden = async context =>
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync(JsonSerializer.Serialize(
                                new ErrorResponse
                                {
                                    Status = StatusCodes.Status403Forbidden,
                                    Title = "Forbidden",
                                    Message = "Permisos insuficientes.",
                                    TraceId = context.HttpContext.TraceIdentifier
                                },
                                new JsonSerializerOptions
                                {
                                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                }));
                        }
                    };
                });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(RoleNames.AdministradorPolicy, policy =>
                    policy.RequireAuthenticatedUser()
                        .RequireRole(RoleNames.Administrador));

                options.AddPolicy(RoleNames.ConsultaStockPolicy, policy =>
                    policy.RequireAuthenticatedUser()
                        .RequireRole(RoleNames.Administrador, RoleNames.Empleado));

                options.AddPolicy(RoleNames.UsuarioAutenticadoPolicy, policy =>
                    policy.RequireAuthenticatedUser());
            });

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