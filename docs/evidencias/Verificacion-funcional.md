# Verificación funcional de Rentavía

Las pruebas se ejecutaron con datos ficticios. Entorno local: Windows, .NET SDK 10.0.400, EF Core e Identity 10.0.11 y SQL Server Express. Las pruebas automatizadas y el recorrido web corresponden al 12 de octubre de 2026.

## Resumen

| Prueba | Resultado | Evidencia |
| --- | --- | --- |
| Compilación de la solución (5 proyectos) | Sin errores ni advertencias | Salida de `dotnet build` |
| Migraciones contra SQL Server | Aplicadas, sin cambios pendientes en el modelo | `dotnet ef migrations has-pending-model-changes` |
| Pruebas automatizadas | 15 de 15 correctas | [Pruebas-automatizadas.json](Pruebas-automatizadas.json) |
| Recorrido HTTP | 87 de 87 correctas | [Recorrido-web.json](Recorrido-web.json) |
| Restricciones SQL | 5 de 5 correctas | [Pruebas-integridad-sql.txt](Pruebas-integridad-sql.txt) |
| Interfaz en escritorio y móvil | Sin errores de JavaScript ni desbordamiento | [ui/resultado.json](ui/resultado.json) |
| Ejecución en Docker | Funcionando | Capturas por agregar en `docker/` |
| Recomendación con OpenAI | Funcionando | Captura por agregar en `ui/08-recomendacion-openai.png` |

## Pruebas automatizadas

Comando: `dotnet run --project tests/Rentavia.Tests -- --sql`. Las pruebas SQL crean una base temporal propia y la eliminan al terminar.

| Caso | Qué verifica |
| --- | --- |
| Tarifa por 24 horas y redondeo | Días y total de una reserva |
| Intervalos adyacentes no se cruzan | Un período que termina cuando otro empieza no es conflicto |
| Rango inválido rechazado | Fin anterior o igual al inicio |
| Versión anterior rechazada | Control de edición concurrente |
| OpenAI valida salida y contrato HTTP | Formato de la petición a la API Responses y lectura de la respuesta |
| ID ajeno de IA provoca respaldo | La IA no puede sugerir un vehículo fuera de los candidatos |
| HTTP 429 y JSON defectuoso | Errores de la API activan el respaldo sin romper la pantalla |
| Timeout e identificador fuera de rango | Respaldo ante espera excesiva o datos inválidos |
| Sin clave o sin candidatos | No se llama a la API cuando no hace falta |
| Dos conexiones simultáneas | Solo una de dos reservas superpuestas se confirma |
| Tarifa histórica, edición y cancelación | La reserva conserva su tarifa; cancelar libera la agenda |
| Mantenimiento bloquea y libera | El vehículo no se puede reservar durante el servicio |
| Entrega, pago, devolución y auditoría | Flujo completo con saldo, cierre y eventos registrados |
| Mantenimiento vencido sin finalizar | El vehículo no se ofrece |
| Edición concurrente con rowversion | SQL Server rechaza la segunda edición |

Las pruebas del adaptador de OpenAI usan respuestas controladas para cubrir casos de error que no se pueden provocar a voluntad con la API real.

## Recorrido HTTP

Comando: `python tests/web_recorrido.py --url http://127.0.0.1:5295`, sobre una instalación de demostración. Las 87 comprobaciones cubren:

- Rutas privadas sin sesión, inicio de sesión de administrador y apertura de todos los listados y formularios.
- Crear, editar y eliminar categorías, sucursales, clientes y vehículos.
- Placa duplicada y edición con versión anterior rechazadas desde el formulario.
- Reserva, entrega, pago, devolución y alquiler cerrado.
- Petición POST sin token antifalsificación rechazada.
- Creación de una cuenta de empleado, cierre de sesión e ingreso como empleado.
- El empleado no puede entrar a Usuarios ni eliminar categorías.

## Capturas de la interfaz

Generadas con `tests/ui-capturas.cjs`.

| Pantalla | Archivo |
| --- | --- |
| Acceso | [01-acceso.png](ui/01-acceso.png) |
| Panel de inicio | [02-panel.png](ui/02-panel.png) |
| Disponibilidad | [03-disponibilidad.png](ui/03-disponibilidad.png) |
| Reservas | [04-reservas.png](ui/04-reservas.png) |
| Recomendador | [05-recomendador.png](ui/05-recomendador.png) |
| Vista móvil | [06-movil.png](ui/06-movil.png) |
| Recomendación en modo respaldo | [07-recomendacion-respaldo.png](ui/07-recomendacion-respaldo.png) |

![Panel de inicio](ui/02-panel.png)

## Docker

La aplicación se ejecuta con `scripts/Iniciar-docker.ps1`: se construye la imagen, SQL Server arranca con su healthcheck, el servicio `setup` aplica migraciones y la web responde en `http://localhost:8080`. La base, las fotografías y las claves de sesión se guardan en volúmenes, de modo que `docker compose down` no las borra; la captura 04 debe mostrarlo.

Capturas por agregar en `docs/evidencias/docker/`:

- `01-build.png`: construcción de la imagen.
- `02-compose-ps.png`: servicios `database` y `web` en ejecución.
- `03-web-docker.png`: la aplicación abierta en `http://localhost:8080`.
- `04-persistencia.png`: datos presentes después de reiniciar los contenedores.

## Recomendador con OpenAI

Con `OPENAI_API_KEY` configurada, el recomendador devuelve sugerencias con origen **OpenAI**, cada una con su explicación, elegidas entre los vehículos disponibles. Cada consulta queda registrada en la tabla `Recomendaciones` con su entrada, candidatos, resultado, origen y modelo.

Evidencia por agregar:

- `ui/08-recomendacion-openai.png`: formulario con preferencias y resultado de origen OpenAI.
- Un registro de la tabla `Recomendaciones` con `Origen = 'OpenAI'` (captura o consulta SQL).

## Cómo repetir las pruebas

```powershell
dotnet run --project tests/Rentavia.Tests -- --sql
python -m pip install -r tests/requirements.txt
python tests/web_recorrido.py --url http://127.0.0.1:5295
sqlcmd -S '.\SQLEXPRESS' -d Rentavia -E -C -b -f 65001 -i database/scripts/Verificar-base.sql
```

El recorrido web crea datos ficticios en la base donde se ejecuta, por eso solo debe usarse sobre una instalación de demostración.
