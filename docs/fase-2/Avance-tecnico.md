# Avance técnico de Rentavía

Estado del desarrollo de la Fase II. Fecha de entrega oficial: 18 de octubre de 2026.

## Resumen

Rentavía está implementado como aplicación ASP.NET Core MVC en cuatro capas, con EF Core sobre SQL Server, autenticación con Identity, recomendador con la API de OpenAI y ejecución en Docker. El flujo completo de la agencia funciona: catálogos, disponibilidad, reserva, entrega, pagos, devolución, mantenimiento, reportes y auditoría.

## Requisitos funcionales

Los requisitos RF-01 a RF-12 vienen de la propuesta de la Fase I.

| Requisito | Implementación | Cómo se probó |
| --- | --- | --- |
| RF-01 Acceso y permisos | Identity con roles Administrador y Empleado; `CuentaController` y `UsuariosController` | Recorrido HTTP: inicio de sesión, rutas protegidas y restricciones del empleado |
| RF-02 Clientes | `ClientesController` con búsqueda, validaciones, control de versión y desactivación | CRUD en el recorrido HTTP y edición concurrente rechazada |
| RF-03 Flota | Vehículos con fotografía y tarifa, categorías y sucursales | CRUD en el recorrido HTTP y placa duplicada rechazada |
| RF-04 Disponibilidad | `AgendaService.Disponibles` sobre la tabla `Ocupaciones` | Prueba de intervalos y prueba SQL de mantenimiento |
| RF-05 Reservas | Crear, editar y cancelar con bloqueo transaccional del vehículo | Prueba de dos conexiones simultáneas y de cancelación |
| RF-06 Entrega | `AlquilerService.EntregarAsync` con inspección inicial y vista imprimible | Prueba SQL y recorrido HTTP |
| RF-07 Devolución y cargos | Inspección final, cargo adicional con concepto y pagos internos | Prueba SQL de pago, saldo, devolución y auditoría |
| RF-08 Mantenimiento | Programar y finalizar sobre la misma agenda | Prueba SQL: bloquea y luego libera |
| RF-09 Correo | No incluido; la propuesta lo dejaba sujeto al alcance del prototipo | No aplica |
| RF-10 Reportes | Pagos, reservas, entregas, costo de mantenimiento y uso por vehículo | Recorrido HTTP con cuenta de administrador |
| RF-11 Recomendación con IA | `OpenAiRecomendador` con API Responses, salida JSON estricta y validación de IDs | Pruebas del adaptador y consulta real con OpenAI |
| RF-12 Auditoría | Evento guardado en la misma transacción de cada operación sensible | Prueba SQL y pantalla de auditoría |

## Integración con OpenAI

El recomendador filtra hasta veinte vehículos disponibles que cumplen capacidad y presupuesto, y envía a OpenAI solo esos candidatos y las preferencias del viaje. El modelo devuelve hasta tres sugerencias con su explicación. El servidor descarta cualquier ID que no esté en la lista. Si la API falla o no hay clave, se muestran los candidatos más económicos marcados como **Reglas (sin IA)**. Cada consulta queda registrada para trazabilidad.

## Docker

La imagen se compila en dos etapas y corre como usuario sin privilegios. Compose levanta SQL Server 2025 con healthcheck, la web y un servicio `setup` para migraciones, cuenta inicial y datos iniciales. La base, las fotos y las claves de sesión persisten en volúmenes. Los secretos se leen de `.env`.

## Base de datos

Tres migraciones: `BaseInicialSqlServer`, `OperacionDeRentas` y `ReferenciaAuditoria`. Once tablas de negocio más las de Identity, con claves foráneas restringidas, índices únicos, restricciones `CHECK` y `rowversion`. El detalle está en el [modelo de datos](../tecnico/Modelo-completo.md).

## Pruebas

15 pruebas automatizadas, 87 comprobaciones HTTP y 5 pruebas de restricciones SQL, todas correctas. Capturas de escritorio y móvil sin errores de JavaScript. Ver [verificación funcional](../evidencias/Verificacion-funcional.md).

## Parámetros del prototipo

Agencia piloto con personal interno, dólares, horario UTC-6 y cobro por bloques de 24 horas redondeados hacia arriba. No hay depósitos, tolerancia de retraso ni margen de limpieza. Estos valores se muestran en la página Acerca del sistema y tendrían que validarse con una agencia real antes de usarse en operación.

## Pendiente para la entrega

- Agregar a `docs/evidencias` las capturas de Docker y de una recomendación de OpenAI.
- Grabar el video de 5 a 10 minutos con la participación de los cinco integrantes.
- Preparar la presentación final.
