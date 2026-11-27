-- Ejecutar manualmente como administrador local.
-- No ejecutar desde la aplicación ni desde el agente.
-- El cliente conserva y muestra las contraseñas aleatorias generadas.
-- Antes de ejecutar: la base y las dos cuentas deben no existir.
-- Este script no modifica cuentas existentes ni usa DROP/IF EXISTS.

CREATE DATABASE inventario_cu02_cu05_cu06_test
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_0900_ai_ci;

CREATE USER 'inventario_integracion_prep'@'127.0.0.1'
    IDENTIFIED BY RANDOM PASSWORD;

CREATE USER 'inventario_integracion'@'127.0.0.1'
    IDENTIFIED BY RANDOM PASSWORD;

GRANT
    CREATE, ALTER, INDEX, REFERENCES,
    SELECT, INSERT, UPDATE, DELETE
ON inventario_cu02_cu05_cu06_test.*
TO 'inventario_integracion_prep'@'127.0.0.1';

GRANT
    SELECT, INSERT, UPDATE, DELETE
ON inventario_cu02_cu05_cu06_test.*
TO 'inventario_integracion'@'127.0.0.1';
