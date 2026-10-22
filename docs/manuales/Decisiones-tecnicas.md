# Decisiones técnicas de Rentavía

Decisiones que tomó el equipo durante la Fase II y su motivo. El detalle de la implementación está en el [manual técnico](Tecnico.md).

| Decisión | Motivo |
| --- | --- |
| ASP.NET Core MVC con Razor, sin frontend separado | La guía pide C# y ASP.NET Core en frontend y backend; un solo proyecto web simplifica el desarrollo y el despliegue |
| SQL Server en lugar de PostgreSQL (propuesto en la Fase I) | Decisión del equipo al iniciar el desarrollo; tiene proveedor oficial de EF Core y Azure SQL Database como opción en la nube |
| Cuatro proyectos (Domain, Application, Infrastructure, Web) | Arquitectura en capas que pide la guía, sin agregar patrones que el tamaño del proyecto no necesita |
| Sin repositorios genéricos, CQRS ni mediadores | El contexto de EF Core ya cumple el papel de repositorio; el código queda más corto y fácil de explicar |
| Servicios solo para operaciones de varias tablas | Reservar, entregar, devolver y pagar necesitan transacciones y reglas compartidas; los CRUD simples no |
| ASP.NET Core Identity | Contraseñas con hash, bloqueo por intentos y roles sin escribir un sistema de acceso propio |
| Tabla única de ocupaciones | Reservas, alquileres y mantenimientos se validan con la misma consulta de cruces |
| Transacción serializable con `UPDLOCK, HOLDLOCK` | Una consulta previa no basta para impedir dos reservas simultáneas; el bloqueo del vehículo sí |
| `rowversion` en entidades editables | Detecta que otro usuario modificó el registro y evita sobrescribir sus cambios |
| Tarifa copiada en la reserva | Un cambio de precio en el catálogo no altera lo acordado |
| Fechas en `datetimeoffset` UTC y dinero en `decimal(12,2)` | Evita errores de zona horaria y de redondeo |
| API de OpenAI desde el backend | La clave queda en el servidor; el navegador nunca la ve |
| La IA solo elige entre candidatos ya filtrados | Las restricciones objetivas (fechas, capacidad, presupuesto) se resuelven con SQL; la IA aporta el criterio sobre preferencias en texto libre |
| Respaldo por reglas cuando falla la IA | La pantalla sigue siendo útil y siempre indica el origen del resultado |
| Migraciones explícitas (`--migrate`) | La web no modifica el esquema al arrancar; en Docker lo hace el servicio `setup` |
| Azure solo como propuesta | La guía no exige el despliegue; la ejecución local se hace con Docker |

## Parámetros de la agencia piloto

Una sucursal piloto, personal interno sin portal de clientes, dólares, horario UTC-6 y cobro por períodos de 24 horas redondeados hacia arriba. No se programaron depósitos, tolerancia de retraso ni margen de limpieza, porque son políticas que cada agencia define.

## Referencias

- [Proveedor SQL Server de EF Core](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/)
- [Migraciones de EF Core](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Transacciones en EF Core](https://learn.microsoft.com/en-us/ef/core/saving/transactions)
- [Manejo de concurrencia en EF Core](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)
- [Autenticación de la API de OpenAI](https://developers.openai.com/api/reference/overview)
