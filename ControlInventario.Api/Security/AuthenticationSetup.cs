using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ControlInventario.Api.Filters;
using ControlInventario.Core.CustomEntities;
using ControlInventario.Core.Interfaces;
using ControlInventario.Core.Security;
using ControlInventario.Services.Interfaces;

namespace ControlInventario.Api.Security
{
    public static class AuthenticationSetup
    {
        public static IServiceCollection AddControlInventarioAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var jwtSettings = configuration
                .GetSection(JwtSettings.SectionName)
                .Get<JwtSettings>() ?? new JwtSettings();
            jwtSettings.Key = configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtSettings.Key)
                || Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32
                || string.IsNullOrWhiteSpace(jwtSettings.Issuer)
                || string.IsNullOrWhiteSpace(jwtSettings.Audience)
                || jwtSettings.ExpirationMinutes <= 0)
            {
                throw new InvalidOperationException(
                    "La configuración JWT debe incluir una clave de al menos 32 bytes, issuer, audience y expiración positiva.");
            }

            services.Configure<JwtSettings>(
                configuration.GetSection(JwtSettings.SectionName));
            services.PostConfigure<JwtSettings>(options =>
            {
                options.Key = configuration["Jwt:Key"];
            });

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

                            var jti = context.Principal?
                                .FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                            if (!string.IsNullOrWhiteSpace(jti)
                                && scope.ServiceProvider.GetRequiredService<IAuthService>()
                                    .IsTokenRevoked(jti))
                            {
                                context.Fail("Token revocado.");
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

            services.AddAuthorization(options =>
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

            return services;
        }
    }
}
