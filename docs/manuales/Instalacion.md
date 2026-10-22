# Manual de instalación de Rentavía

Este manual explica cómo obtener el proyecto, instalar sus dependencias, configurar las variables de entorno y ejecutarlo de dos formas: en Windows con SQL Server local o con Docker.

## 1. Obtener el proyecto

Copia o descomprime la carpeta del proyecto y abre PowerShell en su raíz. Todos los comandos de este manual se ejecutan desde ahí.

## 2. Variables de entorno

La configuración sensible se guarda en un archivo `.env` en la raíz. Ese archivo no se comparte; el proyecto solo incluye la plantilla `.env.example`.

| Variable | Uso |
| --- | --- |
| `MSSQL_SA_PASSWORD` | Contraseña del usuario `sa` de SQL Server en Docker |
| `ADMIN_EMAIL` | Correo de la primera cuenta administradora |
| `ADMIN_PASSWORD` | Contraseña de esa cuenta (mínimo 10 caracteres, con mayúscula, minúscula, número y símbolo) |
| `OPENAI_API_KEY` | Clave de la API de OpenAI para el recomendador |
| `OPENAI_MODEL` | Modelo de OpenAI; por defecto `gpt-4.1-mini` |
| `WEB_PORT` | Puerto de la web en Docker; por defecto 8080 |
| `SQL_PORT` | Puerto de SQL Server en Docker; por defecto 14330 |

Para crear `.env` con contraseñas aleatorias:

```powershell
./scripts/Preparar-env-demo.ps1
```

El script no reemplaza un `.env` existente. Después de crearlo, abre el archivo y escribe tu clave en `OPENAI_API_KEY`. Las contraseñas no deben contener punto y coma ni comillas, porque forman parte de la cadena de conexión.

La clave de OpenAI solo la usa el servidor. No debe aparecer en vistas, JavaScript, capturas ni en la imagen Docker.

## 3. Ejecución con Docker

### Requisitos

- Docker Desktop con contenedores Linux.
- Al menos 2 GB de memoria asignados a Docker, que es el mínimo de la imagen de SQL Server.
- Conexión a Internet para descargar las imágenes y consultar OpenAI.

### Primer arranque

```powershell
./scripts/Iniciar-docker.ps1 -CrearAdministrador -DatosDemo
```

El script realiza estos pasos en orden:

1. Verifica que Docker esté activo y que la configuración de Compose sea válida.
2. Construye la imagen `rentavia-web:local` a partir de `docker/Dockerfile`.
3. Inicia SQL Server 2025 y espera a que su healthcheck responda.
4. Aplica las migraciones de EF Core con el servicio `setup`.
5. Crea la cuenta administradora con `ADMIN_EMAIL` y `ADMIN_PASSWORD`.
6. Carga los datos de demostración (solo si la base está vacía).
7. Inicia la web.

Abre `http://localhost:8080` e ingresa con el correo y la contraseña de `.env`.

### Arranques posteriores y detención

```powershell
./scripts/Iniciar-docker.ps1
docker compose --env-file .env -f docker/docker-compose.yml down
```

El primer comando reconstruye la imagen, aplica migraciones pendientes y vuelve a levantar la web sin tocar los datos. El segundo detiene los contenedores y conserva la base, las fotografías y las claves de sesión, que viven en volúmenes. El comando `down -v` borra esos volúmenes, así que solo debe usarse para empezar desde cero.

### Comandos útiles

```powershell
docker compose --env-file .env -f docker/docker-compose.yml ps
docker compose --env-file .env -f docker/docker-compose.yml logs web
docker compose --env-file .env -f docker/docker-compose.yml logs database
```

## 4. Ejecución local en Windows

### Opción automática

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Iniciar-Rentavia.ps1
```

También funciona con doble clic en `Iniciar-Rentavia.cmd`. Requiere Windows de 64 bits e Internet en la primera ejecución. No hace falta tener instalado Visual Studio ni SQL Server Management Studio.

El lanzador:

1. Comprueba el SDK de .NET indicado en `global.json`. Si falta, lo descarga con el instalador oficial de Microsoft en `.dotnet-local/`, sin modificar el PATH del sistema.
2. Restaura la herramienta `dotnet-ef` y los paquetes NuGet, y compila la solución.
3. Usa SQL Server Express (`.\SQLEXPRESS`) si existe. Si no, crea la instancia `(localdb)\Rentavia` e instala SQL Server Express LocalDB cuando hace falta. Windows puede pedir permisos de administrador en este paso.
4. Crea la base y aplica las migraciones.
5. Crea una cuenta administradora si no hay usuarios. Toma `ADMIN_EMAIL` y `ADMIN_PASSWORD` de `.env` o genera una contraseña aleatoria.
6. Carga los datos de demostración si los catálogos están vacíos.
7. Inicia la web, espera a que `/health` confirme la conexión con SQL Server y abre `http://127.0.0.1:5295`.

La conexión elegida y las credenciales creadas se guardan en `.env.local.json`, que se queda en el equipo local. Las siguientes ejecuciones reutilizan la misma base y las mismas cuentas. Ctrl+C detiene la web.

El lanzador lee `OPENAI_API_KEY` y `OPENAI_MODEL` de `.env`. Si esas variables ya existen en la sesión de PowerShell, tienen prioridad.

| Opción | Uso |
| --- | --- |
| `-SoloPreparar` | Instala, compila y prepara la base sin iniciar la web |
| `-SinDatosDemo` | Prepara la base y la cuenta sin datos ficticios |
| `-SinAbrirNavegador` | Inicia la web sin abrir el navegador |
| `-Puerto 5296` | Usa otro puerto local |
| `-ServidorSql '(localdb)\Rentavia'` | Usa una instancia local concreta |
| `-BaseDatos Rentavia_Demo` | Usa otra base (letras, números y guion bajo) |

### Opción manual

Requisitos: .NET SDK 10.0.400 (o Visual Studio 2026 con desarrollo web) y SQL Server Express en `.\SQLEXPRESS` con autenticación de Windows. Para otra instancia, cambia la cadena de conexión en `src/Rentavia.Web/appsettings.Development.json`.

```powershell
dotnet tool restore
dotnet build Rentavia.sln
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/Rentavia.Web --no-build -- --migrate
./scripts/Crear-administrador.ps1 -Correo 'administrador@agencia.example'
dotnet run --project src/Rentavia.Web --no-build -- --seed-demo
$env:OPENAI_API_KEY = '<tu clave>'
dotnet run --project src/Rentavia.Web --no-build
```

`Crear-administrador.ps1` pide la contraseña sin mostrarla en pantalla. `--seed-demo` solo funciona si no hay vehículos ni clientes. `dotnet run` no lee `.env`, por eso la clave de OpenAI se define como variable de la sesión. La web abre en `http://localhost:5295`.

La aplicación nunca aplica migraciones al arrancar; se aplican con `--migrate` o desde el lanzador.

## 5. Configuración de OpenAI

El servidor lee la clave de `OPENAI_API_KEY` (o `OpenAI__ApiKey`) y el modelo de `OpenAI__Modelo`. En Docker, Compose pasa ambos valores desde `.env`.

Para comprobar la integración, abre Recomendador IA, completa el formulario y revisa el origen del resultado. **OpenAI** indica que respondió el modelo. **Reglas (sin IA)** indica que no hay clave configurada o que la consulta falló; en ese caso el sistema ordena los candidatos por tarifa para que la pantalla siga siendo útil.

## 6. Problemas frecuentes

| Situación | Qué revisar |
| --- | --- |
| No conecta a SQL Server local | Que el servicio de SQL Server esté iniciado y la cadena de conexión apunte a la instancia correcta |
| SQL Server en Docker no arranca | Memoria disponible y `docker compose ... logs database` |
| El inicio de sesión falla | La cuenta queda bloqueada 10 minutos tras cinco intentos fallidos |
| La cuenta demo ya existe | `--create-admin` no reemplaza cuentas; usa la existente o crea otra desde Usuarios |
| No aparecen vehículos disponibles | Sucursal activa, vehículo en estado Activo, tarifa mayor que cero y sin ocupaciones en esas fechas |
| No permite entregar | La reserva debe estar confirmada, iniciar en menos de cinco minutos y el cliente debe tener licencia vigente |
| El recomendador muestra "Reglas (sin IA)" | Clave, saldo de la cuenta de OpenAI, conexión a Internet y nombre del modelo |

## 7. Pruebas

```powershell
dotnet run --project tests/Rentavia.Tests -- --sql
```

Las pruebas crean una base temporal `Rentavia_Pruebas_<GUID>` y la eliminan al terminar; nunca tocan la base `Rentavia`. Sin `--sql` solo se ejecutan las pruebas que no necesitan base de datos. El [manual técnico](Tecnico.md) describe el recorrido web y las capturas automáticas.
