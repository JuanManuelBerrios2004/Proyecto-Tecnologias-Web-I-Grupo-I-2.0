# ControlInventario

API ASP.NET Core .NET 10 con EF Core y MySQL.

## Configuración requerida

Defina la conexión antes de iniciar:

```powershell
$env:ConnectionStrings__ConnectionMySql = "Server=127.0.0.1;Port=3306;Database=<base>;User=<usuario>;Password=<contraseña>"
$env:Jwt__Key = "<clave-sintetica-de-al-menos-32-bytes>"
$env:Jwt__Issuer = "ControlInventario.Api"
$env:Jwt__Audience = "ControlInventario.Client"
$env:Jwt__ExpirationMinutes = "60"
```

No utilice la cuenta `root` ni publique contraseñas o claves.

La documentación OpenAPI está disponible en entorno Development en `/openapi/v1.json`.
