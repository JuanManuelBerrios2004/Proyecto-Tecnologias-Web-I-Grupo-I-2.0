# Integración MySQL local

Archivos:

- `01_prepare_admin.sql`: comandos administrativos que debe ejecutar personalmente el propietario.
- `02_schema_integration.sql`: DDL exacto del proyecto con InnoDB.

Base exclusiva:

`inventario_cu02_cu05_cu06_test`

No debe utilizarse `inventario_db`.

## Antes de ejecutar

Verifique que la base no exista:

```sql
SELECT SCHEMA_NAME
FROM information_schema.SCHEMATA
WHERE SCHEMA_NAME = 'inventario_cu02_cu05_cu06_test';
```

Debe devolver cero filas.

Abra MySQL interactivamente:

```powershell
& "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" `
  --protocol=TCP `
  -h127.0.0.1 `
  -P3306 `
  -uroot `
  -p
```

No coloque la contraseña en el comando ni en un archivo.

Ejecute primero `01_prepare_admin.sql` y reemplace los marcadores de contraseña solo dentro de la sesión interactiva. Después aplique:

```sql
USE inventario_cu02_cu05_cu06_test;
SOURCE D:/tecnologias web/proyecto final/tests/integration/mysql/02_schema_integration.sql;
```

Verifique permisos sin modificar datos:

```sql
SHOW GRANTS FOR 'inventario_integracion_prep'@'127.0.0.1';
SHOW GRANTS FOR 'inventario_integracion'@'127.0.0.1';
```

## Variables externas

La ejecución futura debe recibir:

- `CONTROLINVENTARIO_TEST_MYSQL`
- `CONTROLINVENTARIO_TEST_JWT_KEY`
- `CONTROLINVENTARIO_TEST_JWT_ISSUER`
- `CONTROLINVENTARIO_TEST_JWT_AUDIENCE`
- `CONTROLINVENTARIO_TEST_JWT_EXPIRATION_MINUTES`

Configure la cadena desde PowerShell sin imprimirla:

```powershell
$secure = Read-Host "Cadena MySQL de integración" -AsSecureString
$ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
$plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
[Environment]::SetEnvironmentVariable("CONTROLINVENTARIO_TEST_MYSQL",$plain,"Process")
[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
Remove-Variable plain,secure
```

No configure `ConnectionStrings:ConnectionMySql` ni reutilice credenciales de `inventario_db`.
