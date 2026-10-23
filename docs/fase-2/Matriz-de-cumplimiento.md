# Matriz de cumplimiento de la Fase II

Relación entre lo que pide la [guía de la Fase II](../lineamientos/DSP_Proyecto%20Fase%202_2026.pdf) y lo que contiene el proyecto.

## Productos esperados

| Producto (guía) | Dónde está | Estado |
| --- | --- | --- |
| 1. Aplicación web funcional: navegación, formularios, validaciones, usuarios, CRUD, servicios externos, EF Core | `src/Rentavia.Web`, [manual de usuario](../manuales/Usuario.md) | Completo |
| 2. Backend: capas, CRUD, base de datos, EF Core, validaciones, excepciones, seguridad básica; opcional roles | Cuatro proyectos en `src/`, [manual técnico](../manuales/Tecnico.md) | Completo, con roles y permisos |
| 3. Integración de IA con entrada, procesamiento, resultado y beneficio | `OpenAiRecomendador`, pantalla Recomendador IA, tabla `Recomendaciones` | Completo; falta la captura en evidencias |
| 4. Docker: Dockerfile, docker-compose.yml, variables de entorno | `docker/`, `.env.example`, `scripts/Iniciar-docker.ps1` | Completo; faltan capturas en evidencias |
| 4. Propuesta Azure: servicio, beneficios, costos, arquitectura | [Propuesta Azure](Propuesta-Azure.md) | Completo |
| 5. Integración general del sistema | Flujo de renta conectado a agenda, pagos, auditoría e IA | Completo |
| 6. Pruebas de funcionamiento con evidencia | `database/Verificar-base.sql`, [pruebas en el manual técnico](../manuales/Tecnico.md#11-pruebas), capturas en `docs/evidencias/ui/` | Completo; faltan capturas de Docker y OpenAI |
| 7. Manual de instalación | [Instalacion.md](../manuales/Instalacion.md) | Completo |
| 7. Manual de usuario | [Usuario.md](../manuales/Usuario.md) | Completo |
| 7. Manual técnico | [Tecnico.md](../manuales/Tecnico.md) | Completo |
| 8. Video de 5 a 10 minutos | [Guion](Guion-video.md) | Por grabar |
| 9. Repositorio con `/src`, `/docs`, `/database`, `/docker`, README, `.env.example` | Raíz del proyecto | Completo |
| Documento de avance técnico | [Avance técnico](Avance-tecnico.md) | Completo |

## Rúbrica grupal

| Criterio | Respaldo en el proyecto |
| --- | --- |
| 1. Aplicación web funcional | Todos los módulos operativos y probados en la aplicación |
| 2. Backend y lógica de negocio | `AgendaService` y `AlquilerService` con reglas de agenda, tarifa histórica, saldo y estados |
| 3. Base de datos y EF Core | Migraciones, relaciones, restricciones, índices y control de concurrencia |
| 4. Operaciones CRUD | Categorías, sucursales, clientes y vehículos, con eliminación segura del historial |
| 5. Integración de IA | Recomendador con OpenAI, salida estructurada, validación de IDs y respaldo |
| 6. Integración general | Reserva, entrega, pago, devolución, mantenimiento y recomendación sobre la misma agenda |
| 7. Docker y despliegue | Imagen en dos etapas, Compose con base de datos, volúmenes y servicio de setup; propuesta Azure |
| 8. Diseño UX/UI | Identidad azul y amarillo de los prototipos de la Fase I; diseño adaptable a móvil |
| 9. Pruebas y validación | Pruebas funcionales por módulo, pruebas SQL de restricciones y capturas |
| 10. Documentación técnica | Tres manuales, modelo de datos, avance técnico y matriz |
| 11. Estructura del proyecto | Carpetas pedidas por la guía, README y `.env.example` |
| 12. Presentación final | [Guion](Guion-video.md) con reparto por integrante |

## Opcionales de la guía

La guía marca como opcionales una Web API REST, JWT y roles. El proyecto usa MVC con cookies de Identity y sí implementa roles y permisos, porque el sistema tiene dos perfiles de uso. El despliegue real en Azure también es opcional; se entrega la propuesta documentada.

## Pendientes antes de la defensa

1. Capturas de Docker en `docs/evidencias/docker/`.
2. Captura de una recomendación de origen OpenAI en `docs/evidencias/ui/08-recomendacion-openai.png`.
3. Grabación del video con los cinco integrantes.
