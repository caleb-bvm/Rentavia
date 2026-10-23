# Rentavía

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-ASP.NET_Core_MVC-239120?logo=dotnet&logoColor=white)
![EF Core](https://img.shields.io/badge/EF_Core-10-512BD4)
![SQL Server](https://img.shields.io/badge/SQL_Server-2025-CC2927?logo=microsoftsqlserver&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)
![OpenAI](https://img.shields.io/badge/OpenAI-gpt--4.1--mini-412991?logo=openai&logoColor=white)
![UDB](https://img.shields.io/badge/UDB-DSP_ciclo_02--2026-003366)

> Sistema web para la gestión interna de una agencia de renta de vehículos.
> Proyecto de cátedra de **Desarrollo de Software Propietario** (Universidad Don Bosco, ciclo 02-2026).

## Contenido

- [Descripción](#descripción)
- [Tecnologías](#tecnologías)
- [Módulos](#módulos)
- [Inicio rápido](#inicio-rápido)
- [Estructura del proyecto](#estructura-del-proyecto)
- [Documentación](#documentación)

## Descripción

El personal de la agencia puede:

- Administrar la flota y los clientes.
- Consultar la disponibilidad de vehículos por fechas.
- Crear reservas, entregar y recibir vehículos.
- Registrar pagos y programar mantenimientos.

Además, un **recomendador con OpenAI** sugiere vehículos disponibles según las necesidades del viaje.

## Tecnologías

| Área | Tecnología |
| --- | --- |
| Lenguaje y framework | C# y ASP.NET Core MVC sobre .NET 10 |
| Acceso a datos | Entity Framework Core 10 con SQL Server |
| Autenticación | ASP.NET Core Identity (cuentas y roles) |
| Inteligencia artificial | API Responses de OpenAI (`gpt-4.1-mini` por defecto) |
| Contenedores | Docker y Docker Compose (aplicación y base de datos) |

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

## Inicio rápido

### Con Docker

**Requisito:** Docker Desktop con motor Linux.

Desde la carpeta raíz del proyecto:

1. Genera el archivo de entorno:

   ```powershell
   ./scripts/Preparar-env.ps1
   ```

   Esto crea `.env` con contraseñas aleatorias. El correo y la contraseña del administrador quedan en ese archivo.

2. Abre `.env` y escribe tu clave en `OPENAI_API_KEY`.

3. Inicia la aplicación:

   ```powershell
   ./scripts/Iniciar-docker.ps1 -CrearAdministrador -DatosIniciales
   ```

4. Abre <http://localhost:8080>.

En los siguientes arranques basta con:

```powershell
./scripts/Iniciar-docker.ps1
```

### Sin Docker (Windows)

Haz doble clic en `Iniciar-Rentavia.cmd`.

## Estructura del proyecto

```text
Iniciar-Rentavia.cmd        Doble clic para ejecutar en Windows sin Docker
src/
  Rentavia.Domain/          Entidades del negocio
  Rentavia.Application/     Reglas de cálculo y contrato del recomendador
  Rentavia.Infrastructure/  EF Core, migraciones y servicios (agenda, alquiler, OpenAI)
  Rentavia.Web/             Controladores MVC, formularios y vistas Razor
database/                   Esquema SQL generado, script de verificación y respaldo de datos (datos/)
docker/                     Dockerfile y docker-compose.yml
scripts/                    Lanzador de Windows, Docker, .env, creación de cuentas y exportación de datos
docs/
  fase-1/                   Lineamientos, documento y diagramas de la propuesta
  fase-2/                   Lineamientos, manuales de instalación y técnico, modelo de datos y propuesta Azure
  evidencias/               Capturas de la interfaz
```

## Documentación

### Fase II

- [Manual de instalación](docs/fase-2/Instalacion.md)
- [Manual técnico](docs/fase-2/Tecnico.md) (incluye las [pruebas](docs/fase-2/Tecnico.md#11-pruebas))
- [Modelo de datos](docs/fase-2/Modelo-de-datos.md)
- [Propuesta de despliegue en Azure](docs/fase-2/Propuesta-Azure.md)
- [Lineamientos de la Fase II](<docs/fase-2/DSP_Proyecto Fase 2_2026.pdf>)

> El manual de usuario y el avance técnico forman parte del documento de entrega de la Fase II (secciones 11 y 4 a 9).

### Fase I

- [Documento de la propuesta](<docs/fase-1/Documentación de Proyecto de Cátedra.docx>)
- [Diagramas](docs/fase-1/diagramas/)
- [Lineamientos de la Fase I](<docs/fase-1/DSP_Proyecto Fase 1_2026.pdf>)