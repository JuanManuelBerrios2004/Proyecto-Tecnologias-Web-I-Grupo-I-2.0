using System.Data.Common;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using System.Net;
using AutoMapper;
using ControlInventario.Api.Controllers;
using ControlInventario.Api.Filters;
using ControlInventario.Api.Response;
using ControlInventario.Core.DTOs;
using ControlInventario.Core.Entities;
using ControlInventario.Core.Exceptions;
using ControlInventario.Core.Interfaces;
using ControlInventario.Cu02.Verificaciones;
using ControlInventario.Infrastructure.Data;
using ControlInventario.Infrastructure.Mappings;
using ControlInventario.Infrastructure.Repositories;
using ControlInventario.Services.Interfaces;
using ControlInventario.Services.Services;
using ControlInventario.Services.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using ControlInventario.Core.Security;
using ControlInventario.Services.Integracion;
using ControlInventario.Api.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Json;

const string Invalido = "Errores de validación en los datos enviados.";
const string Precio = "El precio de venta debe ser superior al costo de compra";
const string Duplicado = "Ya existe un producto registrado con ese nombre.";
const string CategoriaInvalida = "La categoría indicada no existe.";
const string ProductoAusente = "Producto no encontrado.";
const string IdentidadInvalida = "Se requiere una identidad de usuario autenticada válida.";
const string Conflicto = "El producto fue modificado o ya no está disponible. Vuelva a consultarlo.";
const string Interno = "Error interno no controlado.";

var registros = new ServiceCollection();
registros.AddAutoMapper(typeof(ProductoProfile).Assembly);
registros.AddScoped<UnidadEnMemoria>();
registros.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UnidadEnMemoria>());
registros.AddTransient<IProductoService, ProductoService>();
registros.AddTransient<ICategoriaService, CategoriaService>();
registros.AddScoped<CrearProductoDtoValidator>();
registros.AddScoped<ActualizarProductoDtoValidator>();
registros.AddScoped<CrearCategoriaDtoValidator>();
registros.AddScoped<ActualizarCategoriaDtoValidator>();
registros.AddScoped<ConsultaStockQueryDtoValidator>();
registros.AddScoped<LoginDtoValidator>();
registros.AddScoped<PasswordHasher<Usuario>>();
registros.AddSingleton<IOptions<JwtSettings>>(Options.Create(new JwtSettings
{
    Key = "clave-de-prueba-segura-32-bytes-minimo",
    Issuer = "Pruebas.ControlInventario",
    Audience = "Pruebas.ControlInventario.Cliente",
    ExpirationMinutes = 30
}));
registros.AddScoped<ITokenService, TokenService>();
registros.AddScoped<IAuthenticationService, AuthenticationService>();
using var proveedor = registros.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
var total = 0;

await Probar("P01 listado y filtros del catálogo", async (api, u, _) =>
{
    u.Productos.Filas.Add(Fila());
    u.Productos.Filas.Add(Fila(12, "Leche", activo: false));
    var lista = ((ApiResponse<IEnumerable<ProductoDto>>)((OkObjectResult)await api.GetProductos(null, null, null)).Value!).Data;
    Exigir(lista.Count() == 2 && lista.Any(p => !p.Activo));
    var activos = ((ApiResponse<IEnumerable<ProductoDto>>)((OkObjectResult)await api.GetProductos("pan", 2, true)).Value!).Data;
    Exigir(activos.Single().Id == 11);
},0);

await Probar("P01 detalle con categoría y 404", async (api, u, _) =>
{
    u.Productos.Filas.Add(Fila());
    var data = (ApiResponse<ProductoDto>)((OkObjectResult)await api.GetProductoById(11)).Value!;
    Exigir(data.Data.CategoriaNombre == "Alimentos");
    await Error(async () => await api.GetProductoById(404), 404, ProductoAusente);
});

await Probar("P02/P08 alta válida y sin historial", async (api, u, _) =>
{
    var dto = JsonSerializer.Deserialize<CrearProductoDto>("""{"Nombre":"  Pan  ","CategoriaId":2,"PrecioCompra":0,"PrecioVenta":1.25,"StockActual":30,"Activo":false,"UsuarioId":5}""")!;
    var creado = (CreatedAtActionResult)await api.InsertProducto(dto);
    var data = (ProductoDto)creado.Value!;
    Exigir(creado.StatusCode == 201 && creado.ActionName == nameof(ProductoController.GetProductoById));
    Exigir((int)creado.RouteValues!["id"]! == data.Id);
    Exigir(data.Nombre == "Pan" && data.StockActual == 0 && data.Activo && data.CategoriaNombre != null);
    Exigir(data.CreadoEn == UnidadEnMemoria.FechaGenerada && u.Historial.Count == 0);
}, guardados: 1);

foreach (var prueba in new (string nombre, CrearProductoDto dto, string campo)[]
{
    ("nombre omitido", new() { CategoriaId=2, PrecioCompra=2m, PrecioVenta=3m }, "Nombre"),
    ("nombre vacío", Alta(nombre:""), "Nombre"),
    ("nombre de espacios", Alta(nombre:" \t "), "Nombre"),
    ("nombre de 101 caracteres", Alta(nombre:new string('x',101)), "Nombre"),
    ("categoría ausente", Alta(categoria:0), "CategoriaId"),
    ("compra omitida", Alta(compra:null), "PrecioCompra"),
    ("venta omitida", Alta(venta:null), "PrecioVenta"),
    ("compra negativa", Alta(compra:-1m), "PrecioCompra"),
    ("venta negativa", Alta(venta:-1m), "PrecioVenta"),
    ("compra con tres decimales", Alta(compra:1.001m), "PrecioCompra"),
    ("venta con tres decimales", Alta(venta:5.001m), "PrecioVenta"),
    ("precio fuera de DECIMAL(10,2)", Alta(venta:100000000m), "PrecioVenta")
})
{
    await Probar("P03/RN-03 " + prueba.nombre, (api, _, _) =>
        Error(() => api.InsertProducto(prueba.dto),400,Invalido,prueba.campo));
}

await Probar("P04 duplicado incluso con otro estado", async (api, u, _) =>
{
    u.Productos.Filas.Add(Fila(activo:false));
    await Error(() => api.InsertProducto(Alta(nombre:"  PAN ")),409,Duplicado);
});
await Probar("P05 venta inferior", (api, _, _) => Error(() => api.InsertProducto(Alta(compra:3m,venta:2m)),400,Invalido,"PrecioVenta",Precio));
await Probar("P06 venta igual", (api, _, _) => Error(() => api.InsertProducto(Alta(compra:3m,venta:3m)),400,Invalido,"PrecioVenta",Precio));
await Probar("P07 alta con categoría inexistente", (api, _, _) => Error(() => api.InsertProducto(Alta(categoria:404)),400,CategoriaInvalida));

await Probar("P09–P10 cambio exclusivo de nombre aun bajo collation equivalente", async (api,u,_) =>
{
    u.Productos.Filas.Add(Fila());
    var dto = (ProductoDto)((OkObjectResult)await api.UpdateProducto(11,Edicion(nombre:"  PAN "))).Value!;
    Exigir(dto.Nombre == "PAN" && dto.Id == 11 && dto.StockActual == 90 && dto.Activo);
    Exigir(dto.CreadoEn == UnidadEnMemoria.FechaGenerada.AddDays(-1));
    Historial(u,"nombre");
},1,1);

foreach (var prueba in new (string campo, ActualizarProductoDto dto)[]
{
    ("categoria_id", Edicion(categoria:3)),
    ("precio_compra", Edicion(compra:3m)),
    ("precio_venta", Edicion(venta:8m))
})
{
    await Probar("P10 cambio exclusivo " + prueba.campo, async (api, u, _) =>
    {
        u.Productos.Filas.Add(Fila());
        await api.UpdateProducto(11,prueba.dto);
        Historial(u,prueba.campo);
    },1,1);
}

await Probar("P11–P13 cuatro cambios, usuario y fecha", async (api,u,_) =>
{
    u.Productos.Filas.Add(Fila(activo:false));
    var dto = JsonSerializer.Deserialize<ActualizarProductoDto>("""{"Id":777,"Nombre":"Galletas","CategoriaId":3,"PrecioCompra":4,"PrecioVenta":8,"StockActual":900,"Activo":true,"UsuarioId":77}""")!;
    var data = (ProductoDto)((OkObjectResult)await api.UpdateProducto(11,dto)).Value!;
    Exigir(data.Id == 11 && data.Nombre == "Galletas" && data.CategoriaId == 3 && data.PrecioCompra == 4m && data.PrecioVenta == 8m);
    Exigir(data.StockActual == 90 && !data.Activo && data.CreadoEn == UnidadEnMemoria.FechaGenerada.AddDays(-1));
    Historial(u,"nombre","categoria_id","precio_compra","precio_venta");
},1,1);

await Probar("P12 datos equivalentes, espacios, decimales y propio nombre", async (api,u,_) =>
{
    u.Productos.Filas.Add(Fila());
    var datos = (ProductoDto)((OkObjectResult)await api.UpdateProducto(11,Edicion(nombre:" Pan ",compra:2.000m,venta:5.00m))).Value!;
    Exigir(datos.Id == 11 && datos.Nombre == "Pan" && u.Historial.Count == 0 && u.Reversiones == 1);
});
await Probar("producto a modificar inexistente", (api, _, _) => Error(() => api.UpdateProducto(404,Edicion()),404,ProductoAusente));
await Probar("edición con nombre de otro producto", async (api,u,_) =>
{
    u.Productos.Filas.Add(Fila());
    u.Productos.Filas.Add(Fila(12,"Otro",false));
    await Error(() => api.UpdateProducto(11,Edicion(nombre:" Otro ")),409,Duplicado);
    Exigir(u.Historial.Count == 0);
});
await Probar("edición con categoría inexistente", async (api,u,_) =>
{
    u.Productos.Filas.Add(Fila());
    await Error(() => api.UpdateProducto(11,Edicion(categoria:404)),400,CategoriaInvalida);
});
await Probar("edición con RN-04 incumplida", (api,_,_) => Error(() => api.UpdateProducto(11,Edicion(compra:5m,venta:5m)),400,Invalido,"PrecioVenta",Precio));
await Probar("edición con precio obligatorio omitido", (api,_,_) => Error(() => api.UpdateProducto(11,Edicion(venta:null)),400,Invalido,"PrecioVenta"));

await Probar("P14–P16 baja, persistencia física y movimientos", async (api,u,_) =>
{
    var fila = Fila();
    fila.Movimientos.Add(new MovimientoInventario { Id = 15 });
    u.Productos.Filas.Add(fila);
    Exigir(await api.DeleteProducto(11) is NoContentResult);
    Exigir(u.Productos.Filas.Single().Id == 11 && !fila.Activo && fila.StockActual == 90);
    Exigir(fila.CategoriaId == 2 && fila.Movimientos.Single().Id == 15);
    Historial(u,"activo");
    Exigir(await api.DeleteProducto(11) is NoContentResult);
    Historial(u,"activo");
},1,1);
await Probar("P14 baja originalmente inactiva", async (api,u,_) =>
{
    u.Productos.Filas.Add(Fila(activo:false));
    Exigir(await api.DeleteProducto(11) is NoContentResult && u.Historial.Count == 0 && u.Reversiones == 1);
});
await Probar("baja de producto inexistente", (api,_,_) => Error(() => api.DeleteProducto(404),404,ProductoAusente));
await Probar("P16 cambio externo de stock sin historial", async (api,u,_) =>
{
    var fila = Fila();
    u.Productos.Filas.Add(fila);
    fila.StockActual = 125;
    var data = (ProductoDto)((OkObjectResult)await api.UpdateProducto(11,Edicion())).Value!;
    Exigir(data.StockActual == 125 && u.Historial.Count == 0);
});

await Probar("P17 identidad ausente, aunque exista cabecera", async (api,u,_) =>
{
    u.Productos.Filas.Add(Fila());
    api.HttpContext.User = new ClaimsPrincipal();
    api.Request.Headers["X-Usuario-Id"] = "27";
    await Error(() => api.UpdateProducto(11,Edicion(nombre:"Cambio")),401,IdentidadInvalida);
    await Error(() => api.DeleteProducto(11),401,IdentidadInvalida);
});
await Probar("P17 claim de identidad no autenticada", (api,_,_) =>
{
    api.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,"27")]));
    return Error(() => api.DeleteProducto(11),401,IdentidadInvalida);
});
await Probar("P17 identificadores inválidos o ambiguos", async (api,_,_) =>
{
    foreach (var claims in new[] { new[]{"abc"}, new[]{"0"}, new[]{"27", "28"}, Array.Empty<string>() })
    {
        api.HttpContext.User = Principal(claims);
        await Error(() => api.UpdateProducto(11,Edicion()),401,IdentidadInvalida);
    }
});
await Probar("P17 usuario autenticado inexistente", async (api,u,_) =>
{
    u.Productos.Filas.Add(Fila());
    api.HttpContext.User = Principal("404");
    await Error(() => api.UpdateProducto(11,Edicion(nombre:"Cambio")),401,IdentidadInvalida);
    await Error(() => api.DeleteProducto(11),401,IdentidadInvalida);
});
await Probar("P17 un claim no autenticado no sustituye al principal", (api,_,_) =>
{
    api.HttpContext.User = new ClaimsPrincipal([
        new ClaimsIdentity([], "Pruebas"),
        new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,"27")])
    ]);
    return Error(() => api.DeleteProducto(11),401,IdentidadInvalida);
});

await Probar("historial y producto se descartan si falla el guardado", async (api,u,_) =>
{
    u.Productos.Filas.Add(Fila());
    u.FalloAlGuardar = new DbUpdateException("Fallo sintético");
    await Error(() => api.UpdateProducto(11,Edicion(nombre:"Otro")),500,Interno);
    Exigir(u.Historial.Count == 0 && u.Productos.Filas.Single().Nombre == "Pan" && u.Reversiones == 1);
},1);

foreach (var prueba in new (int numero, string texto, int estado, string mensaje)[]
{
    (1062,"Duplicate entry 'Otro' for key 'productos.nombre'",409,Duplicado),
    (1062,"Duplicate entry '11' for key 'PRIMARY'",500,Interno),
    (1452,"constraint \u0060fk_productos_categorias\u0060",400,CategoriaInvalida),
    (1452,"constraint \u0060fk_historial_usuarios\u0060",401,IdentidadInvalida),
    (1452,"constraint \u0060fk_historial_productos\u0060",409,Conflicto),
    (1452,"constraint \u0060fk_otra\u0060",500,Interno),
    (3819,"Check constraint 'chk_precio_venta' is violated.",400,Precio),
    (4025,"CONSTRAINT \u0060chk_precio_venta\u0060 failed",400,Precio)
})
{
    await Probar("conflicto MySQL sintético " + prueba.numero + " a " + prueba.estado, async (api,u,_) =>
    {
        u.Productos.Filas.Add(Fila());
        u.FalloAlGuardar = new DbUpdateException("Fallo sintético",MySql(prueba.numero,prueba.texto));
        await Error(() => api.UpdateProducto(11,Edicion(nombre:"Otro")),prueba.estado,prueba.mensaje);
        Exigir(u.Reversiones == 1 && u.Historial.Count == 0 && u.Productos.Filas.Single().Nombre == "Pan");
    },1);
}
await Probar("concurrencia de EF al guardar", async (api,u,_) =>
{
    u.Productos.Filas.Add(Fila());
    u.FalloAlGuardar = new DbUpdateConcurrencyException("Fallo sintético");
    await Error(() => api.DeleteProducto(11),409,Conflicto);
    Exigir(u.Historial.Count == 0 && u.Productos.Filas.Single().Activo);
},1);
await Probar("espera agotada al bloquear la fila", (api,u,_) =>
{
    u.FalloAlBloquear = MySql(1205,"Timeout sintético");
    return Error(() => api.DeleteProducto(11),409,Conflicto);
});

await Probar("compatibilidad con categorías y ambas rutas de stock", async (api,u,sp) =>
{
    u.Productos.Filas.Add(Fila());
    u.Productos.Filas.Add(Fila(12,"Inactivo",false));
    var categorias = new CategoriaService(u);
    await Error(() => categorias.DeleteCategoria(2),409,"No se puede eliminar la categoría porque tiene productos asociados");
    var filtro = new ConsultaStockQueryDto { CategoriaId = 2 };
    var resultado = (ApiResponse<ConsultaStockResponseDto>)((OkObjectResult)await api.GetStockProductos(filtro)).Value!;
    var inventory = ActivatorUtilities.CreateInstance<InventoryController>(sp);
    var stock = (ApiResponse<ConsultaStockResponseDto>)((OkObjectResult)await inventory.GetStock(filtro)).Value!;
    Exigir(resultado.Data.Datos.Single().Id == 11 &&
        stock.Data.Datos.Single().Id == 11 &&
        resultado.Data.Total == 1 &&
        stock.Data.Total == 1);
});

await Probar("CU-05 consulta sin filtros y normaliza nombre", async (api,u,_) =>
{
    u.Productos.Filas.Add(Fila());
    var activoCero = Fila(12,"Teclado",true);
    activoCero.StockActual = 0;
    u.Productos.Filas.Add(activoCero);
    u.Productos.Filas.Add(Fila(13,"Inactivo",false));

    var sinFiltros = (ApiResponse<ConsultaStockResponseDto>)((OkObjectResult)await api.GetStockProductos(new ConsultaStockQueryDto())).Value!;
    Exigir(sinFiltros.Data.Total == 2 &&
        sinFiltros.Data.Datos.Count() == 2 &&
        sinFiltros.Data.Datos.Any(p => p.StockActual == 0));

    var conEspacios = (ApiResponse<ConsultaStockResponseDto>)((OkObjectResult)await api.GetStockProductos(
        new ConsultaStockQueryDto { Nombre = "  tecla  " })).Value!;
    Exigir(conEspacios.Data.Total == 1 &&
        conEspacios.Data.Datos.Single().Nombre == "Teclado");
},0,0);

await Probar("CU-05 combina filtros y excluye inactivos", async (api,u,_) =>
{
    u.Productos.Filas.Add(Fila(11,"Teclado",true));
    u.Productos.Filas.Add(Fila(12,"Teclado gamer",false));
    u.Productos.Filas.Add(Fila(13,"Mouse",true));

    var filtro = new ConsultaStockQueryDto { Nombre = "teclado", CategoriaId = 2 };
    var resultado = (ApiResponse<ConsultaStockResponseDto>)((OkObjectResult)await api.GetStockProductos(filtro)).Value!;
    Exigir(resultado.Data.Total == 1 &&
        resultado.Data.Datos.Single().Nombre == "Teclado");
},0,0);

await Probar("CU-05 devuelve colección vacía y mensaje exacto", async (api,u,_) =>
{
    u.Productos.Filas.Add(Fila(11,"Teclado",true));
    var resultado = (ApiResponse<ConsultaStockResponseDto>)((OkObjectResult)await api.GetStockProductos(
        new ConsultaStockQueryDto { Nombre = "mouse" })).Value!;
    Exigir(resultado.Data.Total == 0 &&
        resultado.Data.Datos != null &&
        !resultado.Data.Datos.Any() &&
        resultado.Data.Mensaje == "No se encontraron productos que coincidan con la búsqueda");
},0,0);

await Probar("CU-05 rechaza categoría, nombre y parámetros incompatibles", async (api,u,sp) =>
{
    var validador = sp.GetRequiredService<ConsultaStockQueryDtoValidator>();
    Exigir(!(await validador.ValidateAsync(new ConsultaStockQueryDto { CategoriaId = 0 })).IsValid);
    Exigir(!(await validador.ValidateAsync(new ConsultaStockQueryDto { CategoriaId = -2 })).IsValid);
    Exigir(!(await validador.ValidateAsync(new ConsultaStockQueryDto { Nombre = new string('x',101) })).IsValid);
    Exigir(!(await validador.ValidateAsync(new ConsultaStockQueryDto { SoloActivos = false })).IsValid);
    Exigir(!(await validador.ValidateAsync(new ConsultaStockQueryDto { StockMinimo = "1" })).IsValid);

    var categoriaInvalida = (BadRequestObjectResult)await api.GetStockProductos(
        new ConsultaStockQueryDto { CategoriaId = 0 });
    Exigir(categoriaInvalida.StatusCode == 400);
},0,0);

await Probar("seguridad: login válido, usuario inactivo y credenciales inválidas", async (_,u,sp) =>
{
    var usuario = u.Usuarios.Filas.Single();
    var hasher = sp.GetRequiredService<PasswordHasher<Usuario>>();
    usuario.PasswordHash = hasher.HashPassword(usuario, "Clave.Prueba.123");
    var auth = sp.GetRequiredService<IAuthenticationService>();

    var respuesta = await auth.AutenticarAsync(new LoginDto
    {
        Email = "  ADMIN@PRUEBA.LOCAL ",
        Password = "Clave.Prueba.123"
    });
    Exigir(respuesta.Token.Length > 20 &&
        respuesta.UsuarioId == 27 &&
        respuesta.Rol == RoleNames.Administrador &&
        !respuesta.Token.Contains(usuario.PasswordHash));

    await Error(
        () => auth.AutenticarAsync(new LoginDto { Email = "nadie@prueba.local", Password = "Clave.Prueba.123" }),
        401,
        "Credenciales inválidas.");
    await Error(
        () => auth.AutenticarAsync(new LoginDto { Email = "admin@prueba.local", Password = "incorrecta" }),
        401,
        "Credenciales inválidas.");

    usuario.Activo = false;
    await Error(
        () => auth.AutenticarAsync(new LoginDto { Email = "admin@prueba.local", Password = "Clave.Prueba.123" }),
        401,
        "Credenciales inválidas.");
},0,0);

await Probar("seguridad: validación y rol desconocido", async (_,u,sp) =>
{
    var validador = sp.GetRequiredService<LoginDtoValidator>();
    Exigir(!(await validador.ValidateAsync(new LoginDto())).IsValid);
    Exigir(!(await validador.ValidateAsync(new LoginDto { Email = "correo", Password = "x" })).IsValid);
    Exigir((await validador.ValidateAsync(new LoginDto { Email = "empleado@prueba.local", Password = "x" })).IsValid);

    u.Usuarios.Filas.Single().Rol!.Nombre = "RolDesconocido";
    u.Usuarios.Filas.Single().PasswordHash =
        sp.GetRequiredService<PasswordHasher<Usuario>>()
            .HashPassword(u.Usuarios.Filas.Single(), "Clave.Prueba.123");

    await Error(
        () => sp.GetRequiredService<IAuthenticationService>().AutenticarAsync(
            new LoginDto { Email = "admin@prueba.local", Password = "Clave.Prueba.123" }),
        401,
        "Credenciales inválidas.");
},0,0);

await Probar("seguridad: JWT válido contiene claims interpretables", (_,u,sp) =>
{
    var usuario = u.Usuarios.Filas.Single();
    usuario.Rol = new Rol { Id = 1, Nombre = "Dueño" };
    var tokenService = sp.GetRequiredService<ITokenService>();
    var (token, expiraEn) = tokenService.Generar(usuario, RoleNames.Administrador);
    Exigir(expiraEn > DateTime.UtcNow);

    var handler = new JwtSecurityTokenHandler
    {
        MapInboundClaims = false
    };
    var settings = sp.GetRequiredService<IOptions<JwtSettings>>().Value;
    var principal = handler.ValidateToken(token, new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key!)),
        ValidateIssuer = true,
        ValidIssuer = settings.Issuer,
        ValidateAudience = true,
        ValidAudience = settings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role
    }, out Microsoft.IdentityModel.Tokens.SecurityToken tokenValidado);

    Exigir(principal.FindAll(ClaimTypes.NameIdentifier).Single().Value == "27");
    Exigir(principal.FindAll(ClaimTypes.Role).Single().Value == RoleNames.Administrador);
    return Task.CompletedTask;
},0,0);

await Probar("seguridad: JWT rechaza expiración, firma, issuer y audience", (_,u,sp) =>
{
    var usuario = u.Usuarios.Filas.Single();
    var settings = sp.GetRequiredService<IOptions<JwtSettings>>().Value;
    var clave = Encoding.UTF8.GetBytes(settings.Key!);
    var credenciales = new SigningCredentials(
        new SymmetricSecurityKey(clave),
        SecurityAlgorithms.HmacSha256);

    var expirado = new JwtSecurityToken(
        issuer: settings.Issuer,
        audience: settings.Audience,
        claims:
        [
            new Claim(ClaimTypes.NameIdentifier, "27"),
            new Claim(ClaimTypes.Role, RoleNames.Administrador)
        ],
        expires: DateTime.UtcNow.AddMinutes(-1),
        signingCredentials: credenciales);

    var parametros = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(clave),
        ValidateIssuer = true,
        ValidIssuer = settings.Issuer,
        ValidateAudience = true,
        ValidAudience = settings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };

    Exigir(!TokenValido(handler, parametros, handler.WriteToken(expirado)));

    var issuerIncorrecto = JwtConClaims(settings, credenciales, usuario,
        "Issuer.Incorrecto", settings.Audience!);
    Exigir(!TokenValido(handler, parametros, issuerIncorrecto));

    var audienceIncorrecta = JwtConClaims(settings, credenciales, usuario,
        settings.Issuer!, "Audience.Incorrecta");
    Exigir(!TokenValido(handler, parametros, audienceIncorrecta));

    var firmaIncorrecta = JwtConClaims(settings,
        new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes("otra-clave-de-prueba-de-32-bytes!")),
            SecurityAlgorithms.HmacSha256),
        usuario,
        settings.Issuer!,
        settings.Audience!);
    Exigir(!TokenValido(handler, parametros, firmaIncorrecta));
    return Task.CompletedTask;
},0,0);

await Probar("seguridad: metadatos protegen CU-02, CU-05 y CU-06", (_,_,_) =>
{
    foreach (var nombre in new[]
    {
        nameof(ProductoController.GetProductos),
        nameof(ProductoController.GetProductoById),
        nameof(ProductoController.InsertProducto),
        nameof(ProductoController.UpdateProducto),
        nameof(ProductoController.DeleteProducto)
    })
    {
        var atributo = typeof(ProductoController).GetMethod(nombre)!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .Single();
        Exigir(atributo.Policy == RoleNames.AdministradorPolicy);
    }

    foreach (var nombre in new[]
    {
        nameof(ProductoController.GetStockProductos),
        nameof(InventoryController.GetStock)
    })
    {
        var metodo = nombre == nameof(ProductoController.GetStockProductos)
            ? typeof(ProductoController).GetMethod(nombre)!
            : typeof(InventoryController).GetMethod(nombre)!;
        var atributo = metodo.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .Single();
        Exigir(atributo.Policy == RoleNames.ConsultaStockPolicy);
    }

    foreach (var nombre in new[]
    {
        nameof(CategoriaController.GetCategorias),
        nameof(CategoriaController.GetCategoriaById)
    })
    {
        var atributo = typeof(CategoriaController).GetMethod(nombre)!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .Single();
        Exigir(atributo.Policy == RoleNames.UsuarioAutenticadoPolicy);
    }

    foreach (var nombre in new[]
    {
        nameof(CategoriaController.InsertCategoria),
        nameof(CategoriaController.UpdateCategoria),
        nameof(CategoriaController.DeleteCategoria)
    })
    {
        var atributo = typeof(CategoriaController).GetMethod(nombre)!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .Single();
        Exigir(atributo.Policy == RoleNames.AdministradorPolicy);
    }
    return Task.CompletedTask;
},0,0);

await Probar("seguridad: login no expone contraseña ni hash", (_,_,sp) =>
{
    var usuario = new Usuario
    {
        Id = 27,
        NombreCompleto = "Usuario",
        PasswordHash = "HASH-PRUEBA",
        Rol = new Rol { Nombre = "Empleado" }
    };
    var hasher = sp.GetRequiredService<PasswordHasher<Usuario>>();
    usuario.PasswordHash = hasher.HashPassword(usuario, "Clave.Prueba.123");
    var json = JsonSerializer.Serialize(new LoginResponseDto
    {
        Token = "TOKEN-PRUEBA",
        ExpiraEn = DateTime.UtcNow.AddMinutes(30),
        UsuarioId = usuario.Id,
        Nombre = usuario.NombreCompleto,
        Rol = "Empleado"
    });
    Exigir(!json.Contains("Password", StringComparison.OrdinalIgnoreCase));
    Exigir(!json.Contains("HASH-PRUEBA", StringComparison.Ordinal));
    Exigir(!json.Contains("Clave.Prueba", StringComparison.Ordinal));
    return Task.CompletedTask;
},0,0);

await Probar("AutoMapper protege stock, fecha, estado y relaciones", (_,_,sp) =>
{
    new MapperConfiguration(cfg => cfg.AddProfile<ProductoProfile>()).AssertConfigurationIsValid();
    var mapper = sp.GetRequiredService<IMapper>();
    var nuevo = mapper.Map<Producto>(Alta());
    Exigir(nuevo.StockActual == 0 && nuevo.Activo && nuevo.CreadoEn == default);
    var existente = Fila(activo:false);
    existente.Movimientos.Add(new MovimientoInventario { Id=9 });
    mapper.Map(Edicion(nombre:"Otro"),existente);
    Exigir(existente.Id == 11 && existente.StockActual == 90 && !existente.Activo);
    Exigir(existente.CreadoEn == UnidadEnMemoria.FechaGenerada.AddDays(-1) && existente.Movimientos.Count == 1);
    return Task.CompletedTask;
});

await Probar("metadatos EF, contexto único y cambio solo de campos editables", async (_,_,_) =>
{
    var options = new DbContextOptionsBuilder<InventarioContext>()
        .UseMySql("Database=verificacion_cu02;Port=1",new MySqlServerVersion(new Version(8,0,36)))
        .AddInterceptors(new BloqueoConexion()).Options;
    using var db = new InventarioContext(options);
    using var work = new UnitOfWork(db);
    var model = db.GetService<IDesignTimeModel>().Model;
    var producto = model.FindEntityType(typeof(Producto))!;
    var historial = model.FindEntityType(typeof(HistorialProducto))!;
    Exigir(producto.GetTableName() == "productos" && historial.GetTableName() == "historial_productos");
    Exigir(producto.GetIndexes().Any(i => i.IsUnique && i.Properties.Single().Name == nameof(Producto.Nombre)));
    Exigir(producto.GetCheckConstraints().Select(x => x.Name).ToHashSet().SetEquals(["chk_precio_venta","chk_stock_positivo"]));
    Exigir(producto.FindProperty(nameof(Producto.PrecioCompra))!.GetColumnType() == "decimal(10,2)");
    Exigir(producto.FindProperty(nameof(Producto.PrecioVenta))!.GetColumnType() == "decimal(10,2)");
    Exigir(historial.FindProperty(nameof(HistorialProducto.CampoModificado))!.GetMaxLength() == 50);
    Exigir(historial.FindProperty(nameof(HistorialProducto.FechaModificacion))!.GetColumnName() == "fecha_modificacion");
    Exigir(historial.FindProperty(nameof(HistorialProducto.FechaModificacion))!.GetDefaultValueSql() == "CURRENT_TIMESTAMP");
    Exigir(historial.GetForeignKeys().Count() == 2 && historial.GetForeignKeys().All(f => f.IsRequired && f.DeleteBehavior == DeleteBehavior.Restrict));
    Exigir(ReferenceEquals(work.HistorialProductoRepository,work.HistorialProductoRepository));
    var actual = Fila();
    db.Attach(actual);
    work.ProductoRepository.UpdateProducto(actual,new Producto { CategoriaId=2,Nombre="Pan",PrecioCompra=2m,PrecioVenta=8m,StockActual=999,Activo=false });
    await work.HistorialProductoRepository.AgregarAsync([new HistorialProducto { ProductoId=11,UsuarioId=27,CampoModificado="precio_venta" }]);
    db.ChangeTracker.DetectChanges();
    var cambios = db.Entry(actual).Properties.Where(e => e.IsModified).Select(e => e.Metadata.Name).ToArray();
    Exigir(cambios.SequenceEqual([nameof(Producto.PrecioVenta)]));
    Exigir(db.ChangeTracker.Entries<HistorialProducto>().Single().State == EntityState.Added);
    var sql = db.Productos.FromSqlInterpolated($"SELECT * FROM productos WHERE id = {11} FOR UPDATE").AsNoTracking().ToQueryString();
    Exigir(sql.Trim().TrimEnd(';').EndsWith("FOR UPDATE",StringComparison.OrdinalIgnoreCase));
    try
    {
        await work.ProductoRepository.ObtenerParaModificarAsync(11);
        throw new InvalidOperationException("Se esperaba el rechazo de una lectura bloqueante sin transacción.");
    }
    catch (InvalidOperationException error) when (error.Message.Contains("requiere una transacción activa",StringComparison.Ordinal))
    {
    }
});

await ProbarCategorias("C01 listado, detalle y lecturas sin guardados", async (u,sp) =>
{
    var service = new CategoriaService(u);
    var categorias = (await service.GetCategoriasAsync()).ToList();
    Exigir(categorias.Count == 2);
    Exigir((await service.GetCategoriaByIdAsync(2)).Nombre == "Alimentos");
    await Error(async () => await service.GetCategoriaByIdAsync(999),404,"Categoría no encontrada.");
    Exigir(u.Guardados == 0);
},0);

await ProbarCategorias("C01 listado vacío", async (u,_) =>
{
    u.Categorias.Filas.Clear();
    var service = new CategoriaService(u);
    Exigir(!(await service.GetCategoriasAsync()).Any());
    Exigir(u.Guardados == 0);
},0);

await ProbarCategorias("C02/C03 creación válida y normalización", async (u,sp) =>
{
    var service = new CategoriaService(u);
    var creada = await service.InsertCategoria(new Categoria
    {
        Nombre = "  Limpieza  ",
        Descripcion = null
    });
    Exigir(creada.Id == 4 && creada.Nombre == "Limpieza");
    Exigir(creada.CreadoEn == UnidadEnMemoria.FechaGenerada);
    Exigir(u.Guardados == 1);

    var validador = sp.GetRequiredService<CrearCategoriaDtoValidator>();
    Exigir(!(await validador.ValidateAsync(new CrearCategoriaDto())).IsValid);
    Exigir(!(await validador.ValidateAsync(new CrearCategoriaDto { Nombre = " " })).IsValid);
    Exigir(!(await validador.ValidateAsync(new CrearCategoriaDto { Nombre = new string('x',51) })).IsValid);
    Exigir(!(await validador.ValidateAsync(new CrearCategoriaDto { Nombre = new string('x',50), Descripcion = new string('d',201) })).IsValid);
    Exigir((await validador.ValidateAsync(new CrearCategoriaDto { Nombre = new string('x',50), Descripcion = new string('d',200) })).IsValid);
},1);

await ProbarCategorias("C04 nombre duplicado y ausencia de guardado", async (u,_) =>
{
    var service = new CategoriaService(u);
    await Error(
        () => service.InsertCategoria(new Categoria { Nombre = "Alimentos" }),
        409,
        "Esta categoría ya se encuentra registrada");
    Exigir(u.Guardados == 0);
},0);

await ProbarCategorias("C05 actualización válida y conservación", async (u,_) =>
{
    var original = u.Categorias.Filas.Single(c => c.Id == 2);
    original.CreadoEn = UnidadEnMemoria.FechaGenerada.AddDays(-1);
    var service = new CategoriaService(u);
    var actualizada = await service.UpdateCategoria(2, new Categoria
    {
        Nombre = "  Abarrotes  ",
        Descripcion = "Productos diversos"
    });
    Exigir(actualizada.Id == 2 &&
        actualizada.Nombre == "Abarrotes" &&
        actualizada.CreadoEn == UnidadEnMemoria.FechaGenerada.AddDays(-1) &&
        u.Guardados == 1);
},1);

await ProbarCategorias("C05 actualización inexistente y nombre de otra categoría", async (u,_) =>
{
    var service = new CategoriaService(u);
    await Error(
        () => service.UpdateCategoria(999, new Categoria { Nombre = "Nueva" }),
        404,
        "Categoría no encontrada.");
    await Error(
        () => service.UpdateCategoria(2, new Categoria { Nombre = "Bebidas" }),
        409,
        "Esta categoría ya se encuentra registrada");
    Exigir(u.Guardados == 0);
},0);

await ProbarCategorias("C06-C08 eliminación, integridad y guardados", async (u,_) =>
{
    var service = new CategoriaService(u);
    await service.DeleteCategoria(2);
    Exigir(!u.Categorias.Filas.Any(c => c.Id == 2));
    Exigir(u.Guardados == 1);
},1);

await ProbarCategorias("C07-C08 eliminación rechazada por productos asociados", async (u,_) =>
{
    u.Productos.Filas.Add(Fila(11,"Activo asociado",true));
    u.Productos.Filas.Add(Fila(12,"Inactivo asociado",false));
    var service = new CategoriaService(u);

    await Error(
        () => service.DeleteCategoria(2),
        409,
        "No se puede eliminar la categoría porque tiene productos asociados");
    Exigir(u.Categorias.Filas.Any(c => c.Id == 2));
    Exigir(u.Productos.Filas.Count == 2);
    Exigir(u.Guardados == 0);
},0);

await ProbarCategorias("C09 inexistente y traducciones MySQL", async (u,_) =>
{
    var service = new CategoriaService(u);
    await Error(
        () => service.DeleteCategoria(999),
        404,
        "Categoría no encontrada.");

    u.FalloAlGuardar = new DbUpdateException(
        "Conflicto sintético",
        MySql(1062,"Duplicate entry 'Alimentos' for key 'IX_categorias_Nombre'"));
    try
    {
        await service.InsertCategoria(new Categoria { Nombre = "DuplicadaSQL" });
        throw new InvalidOperationException("Se esperaba conflicto 1062.");
    }
    catch (BusinessException ex)
    {
        Exigir(ex.StatusCode == HttpStatusCode.Conflict);
        Exigir(ex.Message == "Esta categoría ya se encuentra registrada");
    }

    u.FalloAlGuardar = new DbUpdateException(
        "Conflicto sintético",
        MySql(1451,"Cannot delete or update a parent row: `fk_productos_categorias`"));
    try
    {
        await service.DeleteCategoria(3);
        throw new InvalidOperationException("Se esperaba conflicto 1451.");
    }
    catch (BusinessException ex)
    {
        Exigir(ex.StatusCode == HttpStatusCode.Conflict);
        Exigir(ex.Message == "No se puede eliminar la categoría porque tiene productos asociados");
    }
},2);

await ProbarCategorias("Metadatos EF de categorías", async (u,_) =>
{
    var options = new DbContextOptionsBuilder<InventarioContext>()
        .UseMySql("Database=verificacion_cu06;Port=1",new MySqlServerVersion(new Version(8,0,36)))
        .AddInterceptors(new BloqueoConexion()).Options;
    using var db = new InventarioContext(options);
    var model = db.GetService<IDesignTimeModel>().Model;
    var categoria = model.FindEntityType(typeof(Categoria))!;
    Exigir(categoria.GetTableName() == "categorias");
    Exigir(categoria.FindProperty(nameof(Categoria.Nombre))!.GetMaxLength() == 50);
    Exigir(categoria.FindProperty(nameof(Categoria.Descripcion))!.GetMaxLength() == 200);
    Exigir(categoria.FindProperty(nameof(Categoria.CreadoEn))!.GetDefaultValueSql() == "CURRENT_TIMESTAMP");
    Exigir(categoria.GetIndexes().Any(i => i.IsUnique));
    await Task.CompletedTask;
},0);

await EjecutarPruebasHttpAislado();

await ProbarProteccionMySql("protección MySQL rechaza configuración ausente", () =>
{
    ErrorConfiguracion(() => ConfiguracionMySqlIntegracion.Validar(null));
});

await ProbarProteccionMySql("protección MySQL rechaza cadena inválida", () =>
{
    ErrorConfiguracion(() => ConfiguracionMySqlIntegracion.Validar("cadena=inválida"));
});

await ProbarProteccionMySql("protección MySQL rechaza base incorrecta e inventario_db", () =>
{
    ErrorConfiguracion(() => ConfiguracionMySqlIntegracion.Validar(
        "Server=127.0.0.1;Port=3306;Database=inventario_db;User=inventario_integracion;Password=Prueba"));
    ErrorConfiguracion(() => ConfiguracionMySqlIntegracion.Validar(
        "Server=127.0.0.1;Port=3306;Database=otra_base;User=inventario_integracion;Password=Prueba"));
});

await ProbarProteccionMySql("protección MySQL rechaza servidor remoto", () =>
{
    ErrorConfiguracion(() => ConfiguracionMySqlIntegracion.Validar(
        "Server=db.remoto.local;Port=3306;Database=inventario_cu02_cu05_cu06_test;User=inventario_integracion;Password=Prueba"));
});

await ProbarProteccionMySql("protección MySQL rechaza root y usuarios administrativos", () =>
{
    foreach (var usuario in new[] { "root", "administrator", "admin", "mysql.sys" })
    {
        ErrorConfiguracion(() => ConfiguracionMySqlIntegracion.Validar(
            $"Server=127.0.0.1;Port=3306;Database=inventario_cu02_cu05_cu06_test;User={usuario};Password=Prueba"));
    }
});

await ProbarProteccionMySql("protección MySQL rechaza usuario no autorizado y puerto incorrecto", () =>
{
    ErrorConfiguracion(() => ConfiguracionMySqlIntegracion.Validar(
        "Server=127.0.0.1;Port=3306;Database=inventario_cu02_cu05_cu06_test;User=otro_usuario;Password=Prueba"));
    ErrorConfiguracion(() => ConfiguracionMySqlIntegracion.Validar(
        "Server=127.0.0.1;Port=3307;Database=inventario_cu02_cu05_cu06_test;User=inventario_integracion;Password=Prueba"));
});

await ProbarProteccionMySql("protección MySQL acepta configuración sintética válida", () =>
{
    var builder = ConfiguracionMySqlIntegracion.Validar(
        "Server=127.0.0.1;Port=3306;Database=inventario_cu02_cu05_cu06_test;User=inventario_integracion;Password=Prueba");
    Exigir(builder.Database == ConfiguracionMySqlIntegracion.BaseAutorizada);
    Exigir(builder.Port == ConfiguracionMySqlIntegracion.PuertoAutorizado);
    Exigir(builder.UserID == ConfiguracionMySqlIntegracion.UsuarioAutorizado);
});

await ProbarProteccionMySql("datos sintéticos generan hashes sin contraseñas almacenadas", () =>
{
    var hasher = new PasswordHasher<Usuario>();
    var datos = DatosSinteticos.Preparar(hasher);
    Exigir(datos.Contrasenas.Count == 4);
    Exigir(datos.Roles.Count == 3);
    Exigir(datos.Usuarios.Count == 4);
    Exigir(datos.Categorias.Count == 3);
    Exigir(datos.Productos.Count == 4);
    Exigir(datos.Contrasenas.Values.All(valor =>
        valor.StartsWith("Integracion-Solo-Prueba-", StringComparison.Ordinal)));
});

Console.WriteLine($"TOTAL_APROBADAS={total}; INTEGRACION_MYSQL=PENDIENTE_SIN_CONEXION; HTTP_AISLADO=EJECUTADO");

async Task Probar(string nombre, Func<ProductoController,UnidadEnMemoria,IServiceProvider,Task> caso,int guardados=0,int confirmaciones=0)
{
    using var scope = proveedor.CreateScope();
    var u = scope.ServiceProvider.GetRequiredService<UnidadEnMemoria>();
    var api = ActivatorUtilities.CreateInstance<ProductoController>(scope.ServiceProvider);
    api.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = Principal("27") } };
    try
    {
        await caso(api,u,scope.ServiceProvider);
        Exigir(u.Guardados == guardados && u.Confirmaciones == confirmaciones && !u.EnTransaccion,
            $"Guardados {u.Guardados}/{guardados}, confirmaciones {u.Confirmaciones}/{confirmaciones}");
        total++;
        Console.WriteLine("OK " + nombre);
    }
    catch
    {
        Console.Error.WriteLine("FALLO " + nombre);
        throw;
    }
}

static ClaimsPrincipal Principal(params string[] identificadores) =>
    new(new ClaimsIdentity(identificadores.Select(x => new Claim(ClaimTypes.NameIdentifier,x)),"Pruebas"));

static Producto Fila(int id=11,string nombre="Pan",bool activo=true) => new()
{
    Id=id, Nombre=nombre, CategoriaId=2, Categoria=new Categoria { Id=2,Nombre="Alimentos" },
    PrecioCompra=2m, PrecioVenta=5m, StockActual=90, Activo=activo, CreadoEn=UnidadEnMemoria.FechaGenerada.AddDays(-1)
};

static CrearProductoDto Alta(string? nombre="Pan",int categoria=2,decimal? compra=2m,decimal? venta=5m) => new()
{
    Nombre=nombre, CategoriaId=categoria, PrecioCompra=compra, PrecioVenta=venta
};

static ActualizarProductoDto Edicion(string? nombre="Pan",int categoria=2,decimal? compra=2m,decimal? venta=5m) => new()
{
    Nombre=nombre, CategoriaId=categoria, PrecioCompra=compra, PrecioVenta=venta
};

async Task EjecutarPruebasHttpAislado()
{
    const string key = "clave-sintetica-de-prueba-64-bytes-minimo-abcdefghijklmnopqrstuvwx123456";
    const string issuer = "Pruebas.ControlInventario";
    const string audience = "Pruebas.ControlInventario.Cliente";

    var unit = new UnidadEnMemoria();
    var hasher = new PasswordHasher<Usuario>();
    unit.Usuarios.Filas.Clear();
    unit.Usuarios.Filas.AddRange(
    [
        new Usuario
        {
            Id = 101,
            RolId = 1,
            NombreCompleto = "Administrador sintético",
            Email = "admin@test.local",
            PasswordHash = hasher.HashPassword(new Usuario(), "ClaveSintetica.Admin"),
            Activo = true,
            Rol = new Rol { Id = 1, Nombre = "Administrador" }
        },
        new Usuario
        {
            Id = 102,
            RolId = 2,
            NombreCompleto = "Empleado sintético",
            Email = "empleado@test.local",
            PasswordHash = hasher.HashPassword(new Usuario(), "ClaveSintetica.Empleado"),
            Activo = true,
            Rol = new Rol { Id = 2, Nombre = "Empleado" }
        },
        new Usuario
        {
            Id = 103,
            RolId = 2,
            NombreCompleto = "Usuario inactivo sintético",
            Email = "inactivo@test.local",
            PasswordHash = hasher.HashPassword(new Usuario(), "ClaveSintetica.Inactivo"),
            Activo = false,
            Rol = new Rol { Id = 2, Nombre = "Empleado" }
        },
        new Usuario
        {
            Id = 104,
            RolId = 99,
            NombreCompleto = "Rol desconocido sintético",
            Email = "desconocido@test.local",
            PasswordHash = hasher.HashPassword(new Usuario(), "ClaveSintetica.Desconocido"),
            Activo = true,
            Rol = new Rol { Id = 99, Nombre = "RolDesconocido" }
        }
    ]);
    unit.Productos.Filas.Add(Fila());

    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        EnvironmentName = Environments.Development
    });
    builder.WebHost.UseUrls("http://127.0.0.1:0");
    builder.Configuration["Jwt:Key"] = key;
    builder.Configuration["Jwt:Issuer"] = issuer;
    builder.Configuration["Jwt:Audience"] = audience;
    builder.Configuration["Jwt:ExpirationMinutes"] = "30";

    builder.Services.AddControlInventarioAuthentication(builder.Configuration);
    builder.Services.AddControllers()
        .AddApplicationPart(typeof(ProductoController).Assembly);
    builder.Services.AddAutoMapper(typeof(ProductoProfile).Assembly);
    builder.Services.AddSingleton(unit);
    builder.Services.AddSingleton<IUnitOfWork>(unit);
    builder.Services.AddSingleton<IUsuarioRepository>(unit.UsuarioRepository);
    builder.Services.AddSingleton<IProductoRepository>(unit.ProductoRepository);
    builder.Services.AddSingleton<ICategoriaRepository>(unit.CategoriaRepository);
    builder.Services.AddSingleton<IHistorialProductoRepository>(unit.HistorialProductoRepository);
    builder.Services.AddScoped<IProductoService, ProductoService>();
    builder.Services.AddScoped<ICategoriaService, CategoriaService>();
    builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddSingleton<IOptions<JwtSettings>>(Options.Create(new JwtSettings
    {
        Key = key,
        Issuer = issuer,
        Audience = audience,
        ExpirationMinutes = 30
    }));
    builder.Services.AddScoped<PasswordHasher<Usuario>>();
    builder.Services.AddScoped<CrearProductoDtoValidator>();
    builder.Services.AddScoped<ActualizarProductoDtoValidator>();
    builder.Services.AddScoped<CrearCategoriaDtoValidator>();
    builder.Services.AddScoped<ActualizarCategoriaDtoValidator>();
    builder.Services.AddScoped<ConsultaStockQueryDtoValidator>();
    builder.Services.AddScoped<LoginDtoValidator>();

    await using var app = builder.Build();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapGet("/pruebas/error", (HttpContext _) =>
        throw new InvalidOperationException("Fallo sintético"));

    await app.StartAsync();
    try
    {
        var addresses = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()?.Addresses;
        var baseUrl = addresses?.FirstOrDefault();
        Exigir(!string.IsNullOrWhiteSpace(baseUrl), "El host HTTP aislado no recibió una dirección.");
        using var client = new HttpClient { BaseAddress = new Uri(baseUrl!) };

        string Token(Usuario usuario, string? rol = null, DateTime? expires = null)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.NombreCompleto),
                new Claim(ClaimTypes.Role, rol ?? RoleNames.Normalizar(usuario.Rol?.Nombre)!)
            };
            var token = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires: expires ?? DateTime.UtcNow.AddMinutes(30),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler { MapInboundClaims = false }.WriteToken(token);
        }

        void Bearer(HttpRequestMessage request, string token)
        {
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        var admin = unit.Usuarios.Filas.Single(u => u.Email == "admin@test.local");
        var empleado = unit.Usuarios.Filas.Single(u => u.Email == "empleado@test.local");
        var inactivo = unit.Usuarios.Filas.Single(u => u.Email == "inactivo@test.local");
        var desconocido = unit.Usuarios.Filas.Single(u => u.Email == "desconocido@test.local");
        var adminToken = Token(admin);
        var empleadoToken = Token(empleado);

        await ProbarHttp("HTTP login válido y respuesta sin secretos", async () =>
        {
            using var response = await client.PostAsJsonAsync("/api/Auth/login", new LoginDto
            {
                Email = "admin@test.local",
                Password = "ClaveSintetica.Admin"
            });
            Exigir(response.StatusCode == HttpStatusCode.OK);
            var json = await response.Content.ReadAsStringAsync();
            Exigir(json.Contains("\"token\"", StringComparison.OrdinalIgnoreCase));
            Exigir(!json.Contains("PasswordHash", StringComparison.OrdinalIgnoreCase));
            Exigir(!json.Contains("password_hash", StringComparison.OrdinalIgnoreCase));
            Exigir(!json.Contains("ClaveSintetica", StringComparison.Ordinal));
        });

        await ProbarHttp("HTTP login rechaza credenciales inválidas e inactivo", async () =>
        {
            using var passwordResponse = await client.PostAsJsonAsync("/api/Auth/login", new LoginDto
            {
                Email = "admin@test.local",
                Password = "Incorrecta"
            });
            Exigir(passwordResponse.StatusCode == HttpStatusCode.Unauthorized);

            using var missingUserResponse = await client.PostAsJsonAsync("/api/Auth/login", new LoginDto
            {
                Email = "nadie@test.local",
                Password = "ClaveSintetica.Nadie"
            });
            Exigir(missingUserResponse.StatusCode == HttpStatusCode.Unauthorized);

            using var inactiveResponse = await client.PostAsJsonAsync("/api/Auth/login", new LoginDto
            {
                Email = "inactivo@test.local",
                Password = "ClaveSintetica.Inactivo"
            });
            Exigir(inactiveResponse.StatusCode == HttpStatusCode.Unauthorized);

            using var invalidEmailResponse = await client.PostAsJsonAsync("/api/Auth/login", new LoginDto
            {
                Email = "correo",
                Password = "ClaveSintetica"
            });
            Exigir(invalidEmailResponse.StatusCode == HttpStatusCode.BadRequest);

            using var missingPasswordResponse = await client.PostAsJsonAsync("/api/Auth/login", new LoginDto
            {
                Email = "admin@test.local"
            });
            Exigir(missingPasswordResponse.StatusCode == HttpStatusCode.BadRequest);
        });

        await ProbarHttp("HTTP rechaza token ausente, inválido y HS384", async () =>
        {
            using var noToken = await client.GetAsync("/api/Producto");
            Exigir(noToken.StatusCode == HttpStatusCode.Unauthorized);

            using var invalid = new HttpRequestMessage(HttpMethod.Get, "/api/Producto");
            Bearer(invalid, "token-invalido");
            using var invalidResponse = await client.SendAsync(invalid);
            Exigir(invalidResponse.StatusCode == HttpStatusCode.Unauthorized);

            var hs384 = new JwtSecurityToken(
                issuer,
                audience,
                [
                    new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                    new Claim(ClaimTypes.Role, RoleNames.Administrador)
                ],
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    SecurityAlgorithms.HmacSha384));
            var hs384Token = new JwtSecurityTokenHandler { MapInboundClaims = false }.WriteToken(hs384);
            using var hs384Request = new HttpRequestMessage(HttpMethod.Get, "/api/Producto");
            Bearer(hs384Request, hs384Token);
            using var hs384Response = await client.SendAsync(hs384Request);
            Exigir(hs384Response.StatusCode == HttpStatusCode.Unauthorized);

            using var noneRequest = new HttpRequestMessage(HttpMethod.Get, "/api/Producto");
            Bearer(noneRequest, "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJuYmYiOjB9.");
            using var noneResponse = await client.SendAsync(noneRequest);
            Exigir(noneResponse.StatusCode == HttpStatusCode.Unauthorized);
        });

        await ProbarHttp("HTTP cambio de rol y estado invalida el token vigente", async () =>
        {
            var persistido = unit.Usuarios.Filas.Single(u => u.Id == admin.Id);
            persistido.Rol = new Rol { Id = 2, Nombre = "Empleado" };
            using var changedRoleRequest = new HttpRequestMessage(HttpMethod.Get, "/api/Producto");
            Bearer(changedRoleRequest, adminToken);
            using var changedRoleResponse = await client.SendAsync(changedRoleRequest);
            Exigir(changedRoleResponse.StatusCode == HttpStatusCode.Unauthorized);

            persistido.Rol = new Rol { Id = 1, Nombre = "Administrador" };
            persistido.Activo = false;
            using var inactiveRequest = new HttpRequestMessage(HttpMethod.Get, "/api/Producto");
            Bearer(inactiveRequest, adminToken);
            using var inactiveResponse = await client.SendAsync(inactiveRequest);
            Exigir(inactiveResponse.StatusCode == HttpStatusCode.Unauthorized);

            persistido.Activo = true;
            unit.Usuarios.Filas.Remove(persistido);
            using var deletedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/Producto");
            Bearer(deletedRequest, adminToken);
            using var deletedResponse = await client.SendAsync(deletedRequest);
            Exigir(deletedResponse.StatusCode == HttpStatusCode.Unauthorized);

            unit.Usuarios.Filas.Add(persistido);
        });

        await ProbarHttp("HTTP matrices CU-02 y autorización", async () =>
        {
            using var employeeRequest = new HttpRequestMessage(HttpMethod.Get, "/api/Producto");
            Bearer(employeeRequest, empleadoToken);
            using var employeeResponse = await client.SendAsync(employeeRequest);
            Exigir(employeeResponse.StatusCode == HttpStatusCode.Forbidden);

            using var adminRequest = new HttpRequestMessage(HttpMethod.Get, "/api/Producto");
            Bearer(adminRequest, adminToken);
            using var adminResponse = await client.SendAsync(adminRequest);
            Exigir(adminResponse.StatusCode == HttpStatusCode.OK);

            using var duplicate = await client.PostAsJsonAsync("/api/Producto", new
            {
                Nombre = "Pan",
                CategoriaId = 2,
                PrecioCompra = 2m,
                PrecioVenta = 5m
            });
            Exigir(duplicate.StatusCode == HttpStatusCode.Unauthorized ||
                duplicate.StatusCode == HttpStatusCode.Conflict ||
                duplicate.StatusCode == HttpStatusCode.Created);
        });

        await ProbarHttp("HTTP identidad del historial mediante PUT y DELETE", async () =>
        {
            var persistido = unit.Usuarios.Filas.Single(u => u.Id == admin.Id);
            persistido.Activo = true;
            persistido.Rol = new Rol { Id = 1, Nombre = "Administrador" };
            unit.Productos.Filas.Clear();
            unit.Productos.Filas.Add(Fila());
            unit.Historial.Clear();

            using var put = new HttpRequestMessage(HttpMethod.Put, "/api/Producto/11")
            {
                Content = JsonContent.Create(new
                {
                    Nombre = "Pan modificado",
                    CategoriaId = 2,
                    PrecioCompra = 2m,
                    PrecioVenta = 6m,
                    UsuarioId = 999
                })
            };
            Bearer(put, adminToken);
            using var putResponse = await client.SendAsync(put);
            Exigir(putResponse.StatusCode == HttpStatusCode.OK);
            Exigir(unit.Historial.All(registro => registro.UsuarioId == admin.Id));
            Exigir(unit.Historial.Any(registro => registro.CampoModificado == "nombre"));
            Exigir(unit.Historial.Any(registro => registro.CampoModificado == "precio_venta"));

            using var del = new HttpRequestMessage(HttpMethod.Delete, "/api/Producto/11");
            Bearer(del, adminToken);
            using var delResponse = await client.SendAsync(del);
            Exigir(delResponse.StatusCode == HttpStatusCode.NoContent);
            Exigir(unit.Historial.Any(registro => registro.CampoModificado == "activo"));
        });

        await ProbarHttp("HTTP stock protege ambas rutas para empleado y admin", async () =>
        {
            foreach (var ruta in new[] { "/api/Producto/stock", "/api/inventory/stock" })
            {
                using var noToken = await client.GetAsync(ruta);
                Exigir(noToken.StatusCode == HttpStatusCode.Unauthorized);

                using var employee = new HttpRequestMessage(HttpMethod.Get, ruta);
                Bearer(employee, empleadoToken);
                using var employeeResponse = await client.SendAsync(employee);
                Exigir(employeeResponse.StatusCode == HttpStatusCode.OK);

                using var admin = new HttpRequestMessage(HttpMethod.Get, ruta);
                Bearer(admin, adminToken);
                using var adminResponse = await client.SendAsync(admin);
                Exigir(adminResponse.StatusCode == HttpStatusCode.OK);
            }

            using var invalida = await client.GetAsync("/api/Producto/stock?categoriaId=0");
            Exigir(invalida.StatusCode == HttpStatusCode.Unauthorized ||
                invalida.StatusCode == HttpStatusCode.BadRequest);
        });

        await ProbarHttp("HTTP categorías protege lectura y escrituras", async () =>
        {
            using var employee = new HttpRequestMessage(HttpMethod.Get, "/api/categories");
            Bearer(employee, empleadoToken);
            using var employeeRead = await client.SendAsync(employee);
            Exigir(employeeRead.StatusCode == HttpStatusCode.OK);

            using var employeeWrite = new HttpRequestMessage(HttpMethod.Post, "/api/categories")
            {
                Content = JsonContent.Create(new CrearCategoriaDto
                {
                    Nombre = "Prueba",
                    Descripcion = "Sintética"
                })
            };
            Bearer(employeeWrite, empleadoToken);
            using var employeeWriteResponse = await client.SendAsync(employeeWrite);
            Exigir(employeeWriteResponse.StatusCode == HttpStatusCode.Forbidden);

            using var adminWrite = new HttpRequestMessage(HttpMethod.Post, "/api/categories")
            {
                Content = JsonContent.Create(new CrearCategoriaDto
                {
                    Nombre = "Nueva categoría HTTP",
                    Descripcion = "Sintética"
                })
            };
            Bearer(adminWrite, adminToken);
            using var adminWriteResponse = await client.SendAsync(adminWrite);
            Exigir(adminWriteResponse.StatusCode == HttpStatusCode.Created);
        });

        await ProbarHttp("HTTP middleware devuelve 500 genérico sin detalle interno", async () =>
        {
            using var response = await client.GetAsync("/pruebas/error");
            Exigir(response.StatusCode == HttpStatusCode.InternalServerError);
            var body = await response.Content.ReadAsStringAsync();
            Exigir(!body.Contains("Fallo sintético", StringComparison.OrdinalIgnoreCase));
            Exigir(body.Contains("Error interno no controlado.", StringComparison.OrdinalIgnoreCase));
        });
    }
    finally
    {
        await app.StopAsync();
    }
}

async Task ProbarCategorias(
    string nombre,
    Func<UnidadEnMemoria,IServiceProvider,Task> caso,
    int guardadosEsperados)
{
    using var scope = proveedor.CreateScope();
    var u = scope.ServiceProvider.GetRequiredService<UnidadEnMemoria>();
    try
    {
        await caso(u,scope.ServiceProvider);
        Exigir(u.Guardados == guardadosEsperados &&
            u.Confirmaciones == 0 &&
            !u.EnTransaccion,
            $"Categoría: guardados {u.Guardados}, confirmaciones {u.Confirmaciones}");
        total++;
        Console.WriteLine("OK " + nombre);
    }
    catch
    {
        Console.Error.WriteLine("FALLO " + nombre);
        throw;
    }
}

async Task ProbarHttp(string nombre, Func<Task> caso)
{
    try
    {
        await caso();
        total++;
        Console.WriteLine("OK HTTP " + nombre);
    }
    catch
    {
        Console.Error.WriteLine("FALLO HTTP " + nombre);
        throw;
    }
}

async Task ProbarProteccionMySql(string nombre, Action caso)
{
    try
    {
        await Task.Run(caso);
        total++;
        Console.WriteLine("OK MYSQL " + nombre);
    }
    catch
    {
        Console.Error.WriteLine("FALLO MYSQL " + nombre);
        throw;
    }
}

static void ErrorConfiguracion(Action accion)
{
    var rechazado = false;
    try
    {
        accion();
    }
    catch (InvalidOperationException)
    {
        rechazado = true;
    }
    Exigir(rechazado, "La configuración insegura fue aceptada.");
}

static void Historial(UnidadEnMemoria unidad,params string[] campos)
{
    Exigir(unidad.Historial.Select(x => x.CampoModificado).SequenceEqual(campos));
    Exigir(unidad.Historial.All(x => x.ProductoId == 11 && x.UsuarioId == 27 && x.FechaModificacion == UnidadEnMemoria.FechaGenerada));
}

static async Task Error(Func<Task> accion,int estado,string mensaje,string? campo=null,string? detalle=null)
{
    var contexto = new DefaultHttpContext();
    using var cuerpo = new MemoryStream();
    contexto.Response.Body = cuerpo;
    var middleware = new ExceptionHandlingMiddleware(_ => accion(),NullLogger<ExceptionHandlingMiddleware>.Instance);
    await middleware.InvokeAsync(contexto);
    Exigir(contexto.Response.StatusCode == estado,$"HTTP {contexto.Response.StatusCode}, esperado {estado}");
    using var json = JsonDocument.Parse(cuerpo.ToArray());
    var data = json.RootElement;
    Exigir(data.GetProperty("status").GetInt32() == estado && data.GetProperty("message").GetString() == mensaje);
    if (campo != null)
    {
        Exigir(data.GetProperty("errors").EnumerateArray().Any(e => e.GetProperty("field").GetString() == campo &&
            (detalle == null || e.GetProperty("error").GetString() == detalle)));
    }
}

static bool TokenValido(
    JwtSecurityTokenHandler handler,
    TokenValidationParameters parametros,
    string token)
{
    try
    {
        handler.ValidateToken(token, parametros, out _);
        return true;
    }
    catch (SecurityTokenException)
    {
        return false;
    }
}

static string JwtConClaims(
    JwtSettings settings,
    SigningCredentials credenciales,
    Usuario usuario,
    string issuer,
    string audience)
{
    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims:
        [
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Role, RoleNames.Administrador)
        ],
        expires: DateTime.UtcNow.AddMinutes(30),
        signingCredentials: credenciales);
    return new JwtSecurityTokenHandler { MapInboundClaims = false }.WriteToken(token);
}

static MySqlException MySql(int numero,string mensaje)
{
    // MySqlConnector no ofrece un constructor público para simular errores del servidor.
    var constructor = typeof(MySqlException).GetConstructors(BindingFlags.NonPublic|BindingFlags.Instance)
        .First(x => x.GetParameters().Length >= 3 && x.GetParameters()[0].ParameterType == typeof(MySqlErrorCode) &&
            x.GetParameters().Skip(1).Take(2).All(p => p.ParameterType == typeof(string)));
    var args = constructor.GetParameters().Select(p => p.ParameterType == typeof(MySqlErrorCode) ? (object)(MySqlErrorCode)numero :
        p.Name == "message" ? mensaje : p.Name == "sqlState" ? "23000" : null).ToArray();
    return (MySqlException)constructor.Invoke(args);
}

static void Exigir(bool resultado,string mensaje="Aserción incumplida")
{
    if (!resultado)
        throw new InvalidOperationException(mensaje);
}

internal sealed class BloqueoConexion : DbConnectionInterceptor
{
    public override InterceptionResult ConnectionOpening(DbConnection connection,ConnectionEventData eventData,InterceptionResult result) =>
        throw new InvalidOperationException("Las pruebas no pueden abrir conexiones.");

    public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,ConnectionEventData eventData,
        InterceptionResult result,CancellationToken cancellationToken=default) =>
        throw new InvalidOperationException("Las pruebas no pueden abrir conexiones.");
}
