# Docker de Rentavía

| Archivo | Contenido |
| --- | --- |
| `Dockerfile` | Imagen en dos etapas: compila con el SDK de .NET 10 y ejecuta con el runtime ASP.NET Core como usuario sin privilegios, en el puerto 8080 |
| `docker-compose.yml` | Servicios `database` (SQL Server 2025 Developer), `web` (la aplicación) y `setup` (migraciones, cuenta inicial y datos iniciales) |

## Uso

Desde la raíz del repositorio, con Docker Desktop en modo Linux:

```powershell
./scripts/Preparar-env.ps1
# Escribe tu clave en OPENAI_API_KEY dentro de .env
./scripts/Iniciar-docker.ps1 -CrearAdministrador -DatosIniciales
```

La web abre en `http://localhost:8080`. Las siguientes veces basta con `./scripts/Iniciar-docker.ps1`.

Para detener sin perder datos:

```powershell
docker compose --env-file .env -f docker/docker-compose.yml down
```

`down -v` borra los volúmenes con la base, las fotos y las claves de sesión.

## Detalles

- La web no aplica migraciones al arrancar. Lo hace el servicio `setup` con `--migrate`, que el script ejecuta en cada inicio.
- La web espera a que el healthcheck de SQL Server responda antes de iniciar.
- Los puertos solo se publican en `127.0.0.1`: web en `WEB_PORT` (8080) y SQL en `SQL_PORT` (14330).
- La clave de OpenAI llega como variable de entorno desde `.env` y no se copia a la imagen.
- `Seguridad__RedirigirHttps=false` sirve para la ejecución local por HTTP. Un despliegue público necesita HTTPS.

La explicación completa está en el [manual de instalación](../docs/manuales/Instalacion.md) y en el [manual técnico](../docs/manuales/Tecnico.md).
