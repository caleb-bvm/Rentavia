# Rentavía

Sistema web para la gestión interna de una agencia de renta de vehículos. Es el proyecto de cátedra de Desarrollo de Software Propietario (Universidad Don Bosco, ciclo 02-2026).

El personal de la agencia administra la flota y los clientes, consulta la disponibilidad por fechas, crea reservas, entrega y recibe vehículos, registra pagos y programa mantenimientos. Un recomendador con OpenAI sugiere vehículos disponibles según las necesidades del viaje.

## Tecnologías

- C# y ASP.NET Core MVC sobre .NET 10.
- Entity Framework Core 10 con SQL Server.
- ASP.NET Core Identity para cuentas y roles.
- API Responses de OpenAI (`gpt-4.1-mini` por defecto) para el recomendador.
- Docker y Docker Compose para la aplicación y la base de datos.

## Módulos

| Módulo | Qué hace |
| --- | --- |
| Acceso y usuarios | Inicio de sesión, roles Administrador y Empleado, creación y bloqueo de cuentas |
| Catálogos | CRUD de categorías, sucursales, clientes y vehículos (con fotografía) |
| Disponibilidad | Búsqueda de vehículos libres por fechas, capacidad, categoría y sucursal |
| Reservas | Crear, modificar y cancelar sin cruces de fechas |
| Alquileres | Entrega con inspección, devolución con cargo adicional y pagos internos |
| Mantenimiento | Servicios preventivos y correctivos que bloquean la agenda |
| Recomendador IA | Sugerencias de OpenAI entre los vehículos disponibles, con explicación |
| Reportes y auditoría | Ingresos, reservas, uso de la flota y bitácora de operaciones |

## Ejecución rápida con Docker

Requiere Docker Desktop con motor Linux.

```powershell
# Desde la carpeta raíz del proyecto
./scripts/Preparar-env-demo.ps1
# Abre .env y escribe tu clave en OPENAI_API_KEY
./scripts/Iniciar-docker.ps1 -CrearAdministrador -DatosDemo
```

`Preparar-env-demo.ps1` crea `.env` con contraseñas aleatorias; el correo y la contraseña del administrador quedan en ese archivo. La aplicación abre en `http://localhost:8080`. En los siguientes arranques basta con `./scripts/Iniciar-docker.ps1`.

## Ejecución local en Windows

Doble clic en `Iniciar-Rentavia.cmd`, o desde PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Iniciar-Rentavia.ps1
```

El lanzador instala lo que falte (.NET 10 y SQL Server LocalDB), aplica las migraciones, crea la cuenta administradora y carga datos de demostración. La web abre en `http://127.0.0.1:5295`. Las credenciales generadas quedan en `.env.local.json`, que se queda en el equipo local.

Los pasos manuales, las opciones del lanzador y la configuración de OpenAI están en el [manual de instalación](docs/manuales/Instalacion.md).

## Estructura del proyecto

```text
src/
  Rentavia.Domain/          Entidades del negocio
  Rentavia.Application/     Reglas de cálculo y contrato del recomendador
  Rentavia.Infrastructure/  EF Core, migraciones y servicios (agenda, alquiler, OpenAI)
  Rentavia.Web/             Controladores MVC, formularios y vistas Razor
tests/                      Pruebas automatizadas y recorrido web
database/                   Esquema SQL generado y script de verificación
docker/                     Dockerfile y docker-compose.yml
scripts/                    Arranque local, Docker y creación de cuentas
docs/                       Manuales, documentación técnica y evidencias
```

## Pruebas

```powershell
dotnet run --project tests/Rentavia.Tests -- --sql
```

Ejecuta 15 pruebas de reglas de negocio, del adaptador de OpenAI y de integración con SQL Server. Crea una base temporal y la elimina al terminar. El detalle está en [tests/README.md](tests/README.md).

## Documentación

- [Manual de instalación](docs/manuales/Instalacion.md)
- [Manual de usuario](docs/manuales/Usuario.md)
- [Manual técnico](docs/manuales/Tecnico.md)
- [Avance técnico](docs/fase-2/Avance-tecnico.md)
- [Matriz de cumplimiento de la Fase II](docs/fase-2/Matriz-de-cumplimiento.md)
- [Propuesta de despliegue en Azure](docs/fase-2/Propuesta-Azure.md)
- [Guion del video y la presentación](docs/fase-2/Guion-demostracion.md)
- [Modelo de datos](docs/tecnico/Modelo-completo.md)
- [Evidencias de pruebas](docs/evidencias/Verificacion-funcional.md)

## Equipo

| Integrante | Rol |
| --- | --- |
| Josue Gamaliel Flores Figueroa | Líder de proyecto y análisis |
| Cristian Josué Torres Reyes | Backend e IA |
| Michael Caleb Ortiz Melendez | Frontend y UX/UI |
| Nathaly Ivania Martinez Guerrero | Base de datos y DevOps |
| Jimmy Steeven Peña Saravia | Pruebas y documentación |

Docente: Ing. Rene Mauricio Tejada.

## Alcance

Rentavía es un prototipo académico pensado para una agencia piloto con personal interno. Los clientes no tienen portal propio. Usa dólares, la zona horaria UTC-6 y cobra por períodos de 24 horas redondeados hacia arriba. Los pagos son registros internos, sin conexión con bancos. Los datos de demostración son ficticios.
