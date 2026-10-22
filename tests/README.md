# Pruebas de Rentavía

## Pruebas automatizadas

```powershell
dotnet run --project tests/Rentavia.Tests -- --sql
```

Quince pruebas de reglas de negocio, del adaptador de OpenAI y de integración con SQL Server real. Crean una base temporal `Rentavia_Pruebas_<GUID>` y la eliminan al terminar; la base `Rentavia` no se toca. Sin `--sql` solo corren las pruebas que no necesitan base de datos.

Las pruebas del adaptador de OpenAI usan respuestas HTTP controladas. Así se cubren errores como un HTTP 429, un JSON defectuoso o un timeout, que no se pueden provocar a voluntad con la API real.

Variables opcionales: `RENTAVIA_TEST_CONNECTION` para usar otra instancia de SQL Server y `RENTAVIA_TEST_RESULT` para guardar los resultados en JSON.

## Lanzador de Windows

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tests/Verificar-inicio-local.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tests/Verificar-inicio-local.ps1 -ServidorSql '(localdb)\Rentavia'
```

Requiere .NET y SQL Server instalados. Crea y elimina una base `Rentavia_Inicio_Pruebas_<GUID>` y comprueba migraciones, cuenta inicial, repetición sin duplicados, la opción sin datos iniciales, el arranque HTTP y el inicio de sesión.

## Recorrido HTTP

```powershell
python -m pip install -r tests/requirements.txt
python tests/web_recorrido.py --url http://127.0.0.1:5295
```

Necesita la web en ejecución y `ADMIN_EMAIL` y `ADMIN_PASSWORD` en `.env`. Hace 87 comprobaciones: CRUD de los cuatro catálogos, flujo completo de renta, seguridad y permisos. Deja alquileres y cuentas de prueba en la base, así que debe usarse sobre una base de pruebas.

## Capturas de pantalla

`ui-capturas.cjs` usa Node y Playwright con Chromium. Toma capturas de escritorio y móvil y revisa errores de JavaScript y desbordamiento horizontal. No es una dependencia de la aplicación.

Las capturas quedan en [docs/evidencias/ui](../docs/evidencias/ui/).
