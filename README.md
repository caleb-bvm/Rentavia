# Rentavía

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-ASP.NET_Core_MVC-239120?logo=dotnet&logoColor=white)
![EF Core](https://img.shields.io/badge/EF_Core-10-512BD4)
![SQL Server](https://img.shields.io/badge/SQL_Server-2025-CC2927?logo=microsoftsqlserver&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)
![OpenAI](https://img.shields.io/badge/OpenAI-gpt--4.1--mini-412991?logo=openai&logoColor=white)
![UDB](https://img.shields.io/badge/UDB-DSP_ciclo_02--2026-003366)

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
./scripts/Preparar-env.ps1
# Abre .env y escribe tu clave en OPENAI_API_KEY
./scripts/Iniciar-docker.ps1 -CrearAdministrador -DatosIniciales
```

`Preparar-env.ps1` crea `.env` con contraseñas aleatorias; el correo y la contraseña del administrador quedan en ese archivo. La aplicación abre en `http://localhost:8080`. En los siguientes arranques basta con `./scripts/Iniciar-docker.ps1`.

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

## Documentación

- [Manual de instalación](docs/manuales/Instalacion.md)
- [Manual de usuario](docs/manuales/Usuario.md)
- [Manual técnico](docs/manuales/Tecnico.md)
- [Avance técnico](docs/fase-2/Avance-tecnico.md)
- [Matriz de cumplimiento de la Fase II](docs/fase-2/Matriz-de-cumplimiento.md)
- [Propuesta de despliegue en Azure](docs/fase-2/Propuesta-Azure.md)
- [Guion del video y la presentación](docs/fase-2/Guion-video.md)
- [Modelo de datos](docs/tecnico/Modelo-completo.md)
- [Pruebas automatizadas](tests/README.md)